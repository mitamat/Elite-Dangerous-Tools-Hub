using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

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

    public static string? GetInstalledVersion(string? exePath)
    {
        if (exePath == null || !File.Exists(exePath)) return null;
        try
        {
            var info = FileVersionInfo.GetVersionInfo(exePath);
            return info.ProductVersion ?? info.FileVersion;
        }
        catch { return null; }
    }

    public static async Task<InstallStatus> CheckAsync(Tool tool)
    {
        var installedVersion = GetInstalledVersion(tool.ExePath);
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
        var url = $"https://api.github.com/repos/{repo}/releases/latest";
        var response = await Http.GetAsync(url);
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

            // Skip debug, source, portable (where a proper installer exists)
            if (lower.Contains("debug") || lower.Contains("pdb")) continue;

            var score = 0;

            // Prefer installer formats
            if (lower.EndsWith(".msi")) score += 30;
            else if (lower.EndsWith(".exe") && (lower.Contains("setup") || lower.Contains("install"))) score += 25;
            else if (lower.EndsWith(".exe")) score += 10;
            else if (lower.EndsWith(".zip")) score += 5;
            else continue;

            // Prefer x64 / win64 explicitly
            if (lower.Contains("x64") || lower.Contains("win64") || lower.Contains("64bit")) score += 10;
            if (lower.Contains("win") || lower.Contains("windows")) score += 5;

            // Penalise portable zips and source
            if (lower.Contains("portable")) score -= 8;
            if (lower.Contains("source") || lower.Contains("src")) score -= 20;

            candidates.Add((asset, score));
        }

        return candidates.Count == 0
            ? null
            : candidates.OrderByDescending(c => c.score).First().asset;
    }

    public static async Task DownloadAndInstallAsync(
        string downloadUrl,
        IProgress<(long received, long total)> progress,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(new Uri(downloadUrl).LocalPath);
        var tmp = Path.Combine(Path.GetTempPath(), $"EDHub_install_{Guid.NewGuid()}{ext}");

        try
        {
            await DownloadFileAsync(downloadUrl, tmp, progress, ct);
            RunInstaller(tmp, ext);
        }
        finally
        {
            // Delay deletion so the installer process can start
            _ = Task.Run(async () =>
            {
                await Task.Delay(10_000, CancellationToken.None);
                try { File.Delete(tmp); } catch { /* ignore */ }
            }, CancellationToken.None);
        }
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
        var info = new ProcessStartInfo
        {
            UseShellExecute = true,
            Verb = "runas"
        };

        if (ext.Equals(".msi", StringComparison.OrdinalIgnoreCase))
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
