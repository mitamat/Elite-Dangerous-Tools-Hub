using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Win32;

namespace EDHub;

public record InstallStatus(
    bool IsInstalled,
    string? InstalledVersion,
    string? LatestVersion,
    string? DownloadUrl,
    long DownloadSize)
{
    public bool HasUpdate =>
        IsInstalled &&
        InstalledVersion != null &&
        LatestVersion != null &&
        !NormalizeVersion(InstalledVersion).Equals(NormalizeVersion(LatestVersion), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeVersion(string v) => v.TrimStart('v').Split('+')[0].Split('-')[0];
}

public class InstallService
{
    private static readonly HttpClient Http = new();

    static InstallService()
    {
        Http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("EDHub", "1.0"));
    }

    // ── Path resolution ──────────────────────────────────────────────────────

    public static string? ResolveExePath(Tool tool)
    {
        if (tool.ExePath != null)
        {
            var expanded = Environment.ExpandEnvironmentVariables(tool.ExePath);
            if (File.Exists(expanded)) return expanded;
        }

        if (tool.RegistryHint != null && tool.ExePath != null)
        {
            var exeName = Path.GetFileName(Environment.ExpandEnvironmentVariables(tool.ExePath));
            return FindInRegistry(tool.RegistryHint, exeName);
        }

        return null;
    }

    private static string? FindInRegistry(string hint, string exeName)
    {
        string[] keyPaths =
        [
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        ];

        foreach (var keyPath in keyPaths)
        {
            foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                using var root = hive.OpenSubKey(keyPath);
                if (root == null) continue;

                foreach (var subName in root.GetSubKeyNames())
                {
                    using var sub = root.OpenSubKey(subName);
                    if (sub == null) continue;

                    var displayName = sub.GetValue("DisplayName") as string ?? "";
                    if (!displayName.Contains(hint, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var installLoc = sub.GetValue("InstallLocation") as string;
                    if (string.IsNullOrWhiteSpace(installLoc)) continue;

                    // Try the expected exe name first
                    var candidate = Path.Combine(installLoc, exeName);
                    if (File.Exists(candidate)) return candidate;

                    // Fall back to any exe in the install dir, excluding uninstallers
                    try
                    {
                        var exes = Directory.GetFiles(installLoc, "*.exe", SearchOption.TopDirectoryOnly)
                            .Where(x => !Path.GetFileNameWithoutExtension(x)
                                .Contains("unins", StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                        if (exes.Length == 1) return exes[0];
                        // If multiple, prefer one matching the hint
                        var match = exes.FirstOrDefault(x =>
                            Path.GetFileNameWithoutExtension(x)
                                .Contains(hint.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                        if (match != null) return match;
                    }
                    catch { /* directory may have been moved */ }
                }
            }
        }

        return null;
    }

    // ── Version detection ────────────────────────────────────────────────────

    public static string? GetInstalledVersion(Tool tool)
    {
        var path = ResolveExePath(tool);
        if (path == null) return null;
        try
        {
            var info = FileVersionInfo.GetVersionInfo(path);
            return info.ProductVersion ?? info.FileVersion;
        }
        catch { return null; }
    }

    // ── GitHub release check ─────────────────────────────────────────────────

    public static async Task<InstallStatus> CheckAsync(Tool tool)
    {
        var installedVersion = GetInstalledVersion(tool);
        var isInstalled = installedVersion != null;

        if (tool.GitHubRepo == null)
            return new InstallStatus(isInstalled, installedVersion, null, null, 0);

        try
        {
            var release = await FetchLatestRelease(tool.GitHubRepo);
            if (release == null)
                return new InstallStatus(isInstalled, installedVersion, null, null, 0);

            var tag = release.Value.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "";
            var asset = PickAsset(release.Value);
            var url = asset?.GetProperty("browser_download_url").GetString();
            var size = asset?.GetProperty("size").GetInt64() ?? 0;

            return new InstallStatus(isInstalled, installedVersion, tag, url, size);
        }
        catch
        {
            return new InstallStatus(isInstalled, installedVersion, null, null, 0);
        }
    }

    private static async Task<JsonElement?> FetchLatestRelease(string repo)
    {
        var response = await Http.GetAsync($"https://api.github.com/repos/{repo}/releases/latest");
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement;
    }

    private static JsonElement? PickAsset(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets)) return null;

        var candidates = new List<(JsonElement asset, int score)>();

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            var lower = name.ToLowerInvariant();

            // Skip non-Windows, debug, and source archives
            if (lower.Contains("linux") || lower.Contains("macos") || lower.Contains("darwin") ||
                lower.Contains(".deb") || lower.Contains(".rpm") || lower.Contains(".dmg"))
                continue;
            if (lower.Contains("debug") || lower.Contains(".pdb")) continue;
            if (lower.Contains("source") || lower.Contains(".tar.gz") || lower.Contains(".sum")) continue;

            var score = 0;

            // Format preference: MSI > setup/install exe > plain exe > zip
            if (lower.EndsWith(".msi")) score += 40;
            else if (lower.EndsWith(".exe") && (lower.Contains("setup") || lower.Contains("install") || lower.Contains("installer"))) score += 30;
            else if (lower.EndsWith(".exe")) score += 15;
            else if (lower.EndsWith(".zip")) score += 5;
            else continue;

            // Architecture: prefer x64
            if (lower.Contains("x64") || lower.Contains("amd64") || lower.Contains("win64") || lower.Contains("64bit")) score += 15;
            if (lower.Contains("x86") || lower.Contains("win32") || lower.Contains("32bit")) score -= 5;

            // Prefer Windows-specific assets
            if (lower.Contains("windows") || lower.Contains("win")) score += 5;

            // Penalise unsigned and portable builds
            if (lower.Contains("unsigned")) score -= 20;
            if (lower.Contains("portable")) score -= 10;
            if (lower.Contains("auto-updater")) score += 5; // prefer managed installs

            candidates.Add((asset, score));
        }

        return candidates.Count == 0
            ? null
            : candidates.OrderByDescending(c => c.score).First().asset;
    }

    // ── Download + install ───────────────────────────────────────────────────

    public static async Task DownloadAndInstallAsync(
        Tool tool,
        string downloadUrl,
        IProgress<(long received, long total)> progress,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(new Uri(downloadUrl).LocalPath).ToLowerInvariant();
        var tmp = Path.Combine(Path.GetTempPath(), $"EDHub_install_{Guid.NewGuid()}{ext}");

        try
        {
            await DownloadFileAsync(downloadUrl, tmp, progress, ct);

            if (ext == ".zip")
                ExtractZip(tmp, tool);
            else
                RunInstaller(tmp, ext);
        }
        finally
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(15_000, CancellationToken.None);
                try { File.Delete(tmp); } catch { }
            }, CancellationToken.None);
        }
    }

    private static void ExtractZip(string zipPath, Tool tool)
    {
        string extractDir;

        if (tool.ExePath != null)
        {
            var expanded = Environment.ExpandEnvironmentVariables(tool.ExePath);
            extractDir = Path.GetDirectoryName(expanded)!;
        }
        else
        {
            extractDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                tool.Name);
        }

        Directory.CreateDirectory(extractDir);
        ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);
    }

    private static async Task DownloadFileAsync(
        string url, string dest,
        IProgress<(long, long)> progress,
        CancellationToken ct)
    {
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1;
        using var src = await response.Content.ReadAsStreamAsync(ct);
        using var dst = File.Create(dest);

        var buf = new byte[81920];
        long received = 0;
        int read;
        while ((read = await src.ReadAsync(buf, ct)) > 0)
        {
            await dst.WriteAsync(buf.AsMemory(0, read), ct);
            received += read;
            progress.Report((received, total));
        }
    }

    private static void RunInstaller(string path, string ext)
    {
        var info = new ProcessStartInfo { UseShellExecute = true, Verb = "runas" };

        if (ext == ".msi")
        {
            info.FileName = "msiexec.exe";
            info.Arguments = $"/i \"{path}\"";
        }
        else
        {
            info.FileName = path;
        }

        Process.Start(info);
    }
}
