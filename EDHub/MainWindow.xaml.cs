using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;

namespace EDHub;

public partial class MainWindow : Window
{
    private Tool? _currentTool;
    private InstallStatus? _currentInstallStatus;
    private CancellationTokenSource? _installCts;

    // Image viewer state
    private bool _isDragging;
    private Point _dragStart;
    private double _imageNaturalWidth;
    private double _imageNaturalHeight;
    private const double ZoomMin = 0.1;
    private const double ZoomMax = 8.0;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await MainWebView.EnsureCoreWebView2Async();
            BuildDashboard();
        };
        ImageViewerBorder.SizeChanged += (_, _) => { if (_currentTool?.Type == ToolType.Image) FitImage(); };
    }

    private void BuildDashboard()
    {
        foreach (var (id, tool) in Tools.All)
        {
            var card = new Border
            {
                Width = 218, Height = 144, Margin = new Thickness(0, 0, 12, 12),
                Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x32)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x3F, 0x55)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand
            };
            var stack = new StackPanel { Margin = new Thickness(16) };
            stack.Children.Add(new TextBlock { Text = tool.Icon, FontSize = 24, Margin = new Thickness(0, 0, 0, 8) });
            stack.Children.Add(new TextBlock
            {
                Text = tool.Name, FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8))
            });
            stack.Children.Add(new TextBlock
            {
                Text = tool.Desc, FontSize = 11, TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x99, 0xAA))
            });
            card.Child = stack;

            var capturedId = id;
            card.MouseDown += (_, _) => OpenTool(capturedId);
            card.MouseEnter += (_, _) => card.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x2B));
            card.MouseLeave += (_, _) => card.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x3F, 0x55));

            ToolCardPanel.Children.Add(card);
        }
    }

    private void NavItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
            OpenTool(id);
    }

    private void OpenTool(string id)
    {
        if (!Tools.All.TryGetValue(id, out var tool)) return;
        _currentTool = tool;

        switch (tool.Type)
        {
            case ToolType.Web:     ShowWebView(tool);     break;
            case ToolType.Desktop: ShowDesktopPanel(tool); break;
            case ToolType.Image:   ShowImageViewer(tool);  break;
        }
    }

    private void ShowWebView(Tool tool)
    {
        DashboardView.Visibility = Visibility.Collapsed;
        DesktopAppPanel.Visibility = Visibility.Collapsed;
        ImageViewerPanel.Visibility = Visibility.Collapsed;
        WebViewPanel.Visibility = Visibility.Visible;

        WebViewTitle.Text = tool.FullName;
        MainWebView.Source = new Uri(tool.Url!);
    }

    private void ShowDesktopPanel(Tool tool)
    {
        DashboardView.Visibility = Visibility.Collapsed;
        WebViewPanel.Visibility = Visibility.Collapsed;
        ImageViewerPanel.Visibility = Visibility.Collapsed;
        DesktopAppPanel.Visibility = Visibility.Visible;

        DesktopAppTitle.Text = tool.FullName;
        DesktopAppIcon.Text = tool.Icon;
        DesktopAppName.Text = tool.FullName;
        DesktopAppDesc.Text = tool.Desc;
        InstallNote.Text = tool.InstallNote ?? string.Empty;
        GitHubBtn.Visibility = tool.Github != null ? Visibility.Visible : Visibility.Collapsed;

        // Reset install UI
        _currentInstallStatus = null;
        InstallStatusText.Text = tool.GitHubRepo != null ? "Checking for updates…" : string.Empty;
        InstallBtn.Visibility = Visibility.Collapsed;
        UpdateBtn.Visibility = Visibility.Collapsed;
        DownloadProgress.Visibility = Visibility.Collapsed;
        DownloadStatusText.Visibility = Visibility.Collapsed;

        if (tool.GitHubRepo != null)
            _ = CheckInstallStatusAsync(tool);
    }

    private async Task CheckInstallStatusAsync(Tool tool)
    {
        var status = await InstallService.CheckAsync(tool);
        _currentInstallStatus = status;

        Dispatcher.Invoke(() =>
        {
            if (_currentTool != tool) return;

            if (status.LatestVersion == null)
            {
                InstallStatusText.Text = status.IsInstalled
                    ? $"Installed — could not reach GitHub"
                    : "Not installed — could not reach GitHub";
                if (!status.IsInstalled)
                    InstallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0x66, 0x33));
                return;
            }

            if (!status.IsInstalled)
            {
                InstallStatusText.Text = $"Not installed  ·  Latest: v{status.LatestVersion}";
                InstallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x99, 0xAA));
                InstallBtn.Visibility = Visibility.Visible;
            }
            else if (status.HasUpdate)
            {
                InstallStatusText.Text = $"Installed v{status.InstalledVersion}  ·  Update: v{status.LatestVersion}";
                InstallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xCC, 0x66));
                UpdateBtn.Visibility = Visibility.Visible;
            }
            else
            {
                InstallStatusText.Text = $"Installed v{status.InstalledVersion}  ·  Up to date";
                InstallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xCC, 0x66));
            }
        });
    }

    private void ShowImageViewer(Tool tool)
    {
        DashboardView.Visibility = Visibility.Collapsed;
        WebViewPanel.Visibility = Visibility.Collapsed;
        DesktopAppPanel.Visibility = Visibility.Collapsed;
        ImageViewerPanel.Visibility = Visibility.Visible;

        ImageViewerTitle.Text = tool.FullName;

        var bitmap = new BitmapImage(new Uri(tool.ImageResource!));
        DiagramImage.Source = bitmap;

        // Store natural dimensions once loaded
        bitmap.DownloadCompleted += (_, _) => { _imageNaturalWidth = bitmap.PixelWidth; _imageNaturalHeight = bitmap.PixelHeight; FitImage(); };
        if (bitmap.PixelWidth > 0)
        {
            _imageNaturalWidth = bitmap.PixelWidth;
            _imageNaturalHeight = bitmap.PixelHeight;
            FitImage();
        }
    }

    // ── Image viewer: fit, zoom, pan ──────────────────────────────────────

    private void FitImage()
    {
        if (_imageNaturalWidth == 0 || ImageViewerBorder.ActualWidth == 0) return;

        var scaleX = ImageViewerBorder.ActualWidth / _imageNaturalWidth;
        var scaleY = ImageViewerBorder.ActualHeight / _imageNaturalHeight;
        var scale = Math.Min(scaleX, scaleY);

        DiagramImage.Width = _imageNaturalWidth;
        DiagramImage.Height = _imageNaturalHeight;

        ImageScale.ScaleX = scale;
        ImageScale.ScaleY = scale;

        // Centre the image
        var scaledW = _imageNaturalWidth * scale;
        var scaledH = _imageNaturalHeight * scale;
        ImageTranslate.X = (ImageViewerBorder.ActualWidth - scaledW) / 2;
        ImageTranslate.Y = (ImageViewerBorder.ActualHeight - scaledH) / 2;

        UpdateZoomLabel(scale);
    }

    private void ImageViewer_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var factor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
        var currentScale = ImageScale.ScaleX;
        var newScale = Math.Clamp(currentScale * factor, ZoomMin, ZoomMax);
        if (Math.Abs(newScale - currentScale) < 0.001) return;

        // Zoom toward the mouse cursor position
        var mouse = e.GetPosition(ImageViewerBorder);
        var imgX = (mouse.X - ImageTranslate.X) / currentScale;
        var imgY = (mouse.Y - ImageTranslate.Y) / currentScale;

        ImageScale.ScaleX = newScale;
        ImageScale.ScaleY = newScale;
        ImageTranslate.X = mouse.X - imgX * newScale;
        ImageTranslate.Y = mouse.Y - imgY * newScale;

        UpdateZoomLabel(newScale);
        e.Handled = true;
    }

    private void Image_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStart = e.GetPosition(ImageViewerBorder);
        DiagramImage.CaptureMouse();
        DiagramImage.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void Image_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        DiagramImage.ReleaseMouseCapture();
        DiagramImage.Cursor = Cursors.Arrow;
    }

    private void Image_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var pos = e.GetPosition(ImageViewerBorder);
        ImageTranslate.X += pos.X - _dragStart.X;
        ImageTranslate.Y += pos.Y - _dragStart.Y;
        _dragStart = pos;
    }

    private void ImageFit_Click(object sender, RoutedEventArgs e) => FitImage();

    private void UpdateZoomLabel(double scale) =>
        ImageZoomLabel.Text = $"{scale * 100:F0}%";

    // ── Standard navigation ───────────────────────────────────────────────

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _installCts?.Cancel();
        _currentTool = null;
        _currentInstallStatus = null;
        WebViewPanel.Visibility = Visibility.Collapsed;
        DesktopAppPanel.Visibility = Visibility.Collapsed;
        ImageViewerPanel.Visibility = Visibility.Collapsed;
        DashboardView.Visibility = Visibility.Visible;
        MainWebView.Source = new Uri("about:blank");
    }

    private void OpenExternal_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTool?.Url != null)
            Process.Start(new ProcessStartInfo(_currentTool.Url) { UseShellExecute = true });
    }

    private void LaunchApp_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTool == null) return;
        var path = InstallService.ResolveExePath(_currentTool);
        if (path == null)
        {
            MessageBox.Show(
                $"Could not locate {_currentTool.Name}.\n\nMake sure the application is installed.",
                "Launch Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not launch {_currentTool.Name}.\n\n{ex.Message}",
                "Launch Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTool?.Github != null)
            Process.Start(new ProcessStartInfo(_currentTool.Github) { UseShellExecute = true });
    }

    private async void InstallBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentInstallStatus?.DownloadUrl == null) return;

        var tool = _currentTool;
        var url = _currentInstallStatus.DownloadUrl;
        var totalBytes = _currentInstallStatus.DownloadSize;

        InstallBtn.Visibility = Visibility.Collapsed;
        UpdateBtn.Visibility = Visibility.Collapsed;
        DownloadProgress.Value = 0;
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadStatusText.Text = "Starting download…";
        DownloadStatusText.Visibility = Visibility.Visible;
        InstallStatusText.Text = "Downloading…";

        _installCts?.Cancel();
        _installCts = new CancellationTokenSource();
        var ct = _installCts.Token;

        var progress = new Progress<(long received, long total)>(t =>
        {
            var (recv, tot) = t;
            var effective = tot > 0 ? tot : totalBytes;
            if (effective > 0)
                DownloadProgress.Value = recv * 100.0 / effective;
            DownloadStatusText.Text = effective > 0
                ? $"{recv / 1_048_576.0:F1} MB / {effective / 1_048_576.0:F1} MB"
                : $"{recv / 1_048_576.0:F1} MB downloaded";
        });

        try
        {
            await InstallService.DownloadAndInstallAsync(tool!, url, progress, ct);
            DownloadStatusText.Text = "Installer launched — follow the prompts.";
            DownloadProgress.Visibility = Visibility.Collapsed;
            InstallStatusText.Text = "Installation in progress…";
            InstallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x99, 0xAA));
        }
        catch (OperationCanceledException)
        {
            DownloadProgress.Visibility = Visibility.Collapsed;
            DownloadStatusText.Visibility = Visibility.Collapsed;
            InstallStatusText.Text = "Download cancelled.";
        }
        catch (Exception ex)
        {
            DownloadProgress.Visibility = Visibility.Collapsed;
            DownloadStatusText.Text = $"Error: {ex.Message}";
            InstallStatusText.Text = "Download failed.";
            MessageBox.Show($"Download failed:\n{ex.Message}", "Install Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
