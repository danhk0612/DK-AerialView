using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DKAerialView.Models;
using DKAerialView.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace DKAerialView;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly OpenRouterImageService _openRouterImageService = new();
    private AppSettings _settings = new();
    private bool _webReady;
    private bool _syncingCamera;
    private bool _processing;
    private double _latitude = 37.5665;
    private double _longitude = 126.9780;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _settings = await _settingsService.LoadAsync();
        await InitializeMapAsync();
    }

    private async Task InitializeMapAsync()
    {
        StatusText.Text = "지도 초기화 중...";
        await MapWebView.EnsureCoreWebView2Async();
        MapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        var htmlPath = Path.Combine(AppContext.BaseDirectory, "Assets", "MapHost", "index.html");
        MapWebView.Source = new Uri(htmlPath);
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        using var doc = JsonDocument.Parse(e.WebMessageAsJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeNode)) return;

        switch (typeNode.GetString())
        {
            case "hostReady":
                await SendInitializeAsync();
                break;
            case "ready":
                _webReady = true;
                UpdateActionButtons();
                StatusText.Text = "Google 지도 준비됨";
                await SendCameraAsync();
                await SendFrameAsync();
                break;
            case "cameraChanged":
                if (root.TryGetProperty("lat", out var lat)) _latitude = lat.GetDouble();
                if (root.TryGetProperty("lng", out var lng)) _longitude = lng.GetDouble();
                _syncingCamera = true;
                if (root.TryGetProperty("zoom", out var zoom)) ZoomSlider.Value = zoom.GetDouble();
                if (root.TryGetProperty("tilt", out var tilt)) TiltSlider.Value = tilt.GetDouble();
                if (root.TryGetProperty("heading", out var heading)) HeadingSlider.Value = heading.GetDouble();
                _syncingCamera = false;
                break;
            case "geocodeResult":
                if (root.TryGetProperty("formattedAddress", out var formatted))
                    StatusText.Text = formatted.GetString() ?? "주소 검색 완료";
                break;
            case "error":
                StatusText.Text = root.TryGetProperty("message", out var message) ? message.GetString() ?? "지도 오류" : "지도 오류";
                break;
        }
    }

    private Task SendInitializeAsync()
    {
        var command = new { type = "initialize", provider = "google", apiKey = _settings.GoogleMapsApiKey };
        MapWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(command));
        return Task.CompletedTask;
    }

    private Task SendCameraAsync()
    {
        if (!_webReady || _syncingCamera) return Task.CompletedTask;
        var command = new
        {
            type = "setCamera",
            lat = _latitude,
            lng = _longitude,
            zoom = ZoomSlider.Value,
            tilt = TiltSlider.Value,
            heading = HeadingSlider.Value
        };
        MapWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(command));
        return Task.CompletedTask;
    }

    private Task SendFrameAsync()
    {
        if (!_webReady || !TryGetOutputSize(out var width, out var height)) return Task.CompletedTask;
        var command = new { type = "setFrame", width, height };
        MapWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(command));
        return Task.CompletedTask;
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAddressAsync();

    private async void AddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await SearchAddressAsync();
    }

    private Task SearchAddressAsync()
    {
        if (!_webReady || string.IsNullOrWhiteSpace(AddressBox.Text)) return Task.CompletedTask;
        var command = new { type = "searchAddress", address = AddressBox.Text.Trim() };
        MapWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(command));
        StatusText.Text = "주소 검색 중...";
        return Task.CompletedTask;
    }

    private async void Camera_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IsLoaded) await SendCameraAsync();
    }

    private async void CameraPresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || CameraPresetBox.SelectedItem is not ComboBoxItem item || item.Tag is not string tag) return;
        var values = tag.Split(',');
        if (values.Length != 3) return;
        _syncingCamera = true;
        ZoomSlider.Value = double.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture);
        TiltSlider.Value = double.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
        HeadingSlider.Value = double.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture);
        _syncingCamera = false;
        await SendCameraAsync();
    }

    private async void OutputPresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) await SendFrameAsync();
    }

    private void ProviderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) StatusText.Text = "현재 1차 버전은 Google Provider만 활성화되어 있습니다.";
    }

    private async void Capture_Click(object sender, RoutedEventArgs e)
    {
        if (!_webReady || _processing || !TryGetOutputSize(out var targetWidth, out var targetHeight)) return;

        var dialog = new SaveFileDialog
        {
            Title = "조감도 원본 저장",
            Filter = "PNG 이미지 (*.png)|*.png",
            FileName = $"DK-AerialView-{DateTime.Now:yyyyMMdd-HHmmss}.png",
            DefaultExt = ".png"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            SetProcessing(true);
            StatusText.Text = "현재 프레임 캡처 중...";
            var imageBytes = await CaptureFrameAsync(targetWidth, targetHeight);
            await File.WriteAllBytesAsync(dialog.FileName, imageBytes);
            StatusText.Text = $"캡처 저장 완료: {targetWidth} × {targetHeight}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"캡처 실패: {ex.Message}";
        }
        finally
        {
            SetProcessing(false);
        }
    }

    private async void Enhance_Click(object sender, RoutedEventArgs e)
    {
        if (!_webReady || _processing || !TryGetOutputSize(out var targetWidth, out var targetHeight)) return;
        if (string.IsNullOrWhiteSpace(_settings.OpenRouterApiKey))
        {
            StatusText.Text = "설정에서 OpenRouter API Key를 입력하세요.";
            return;
        }
        if (string.IsNullOrWhiteSpace(_settings.OpenRouterModel))
        {
            StatusText.Text = "설정에서 OpenRouter 이미지 모델을 지정하세요.";
            return;
        }

        try
        {
            SetProcessing(true);
            StatusText.Text = "AI용 원본 프레임 캡처 중...";
            var sourceBytes = await CaptureFrameAsync(targetWidth, targetHeight);

            var resolution = Math.Max(targetWidth, targetHeight) >= 3000 ? "4K" : "2K";
            var aspectRatio = GetAspectRatio(targetWidth, targetHeight);
            StatusText.Text = $"OpenRouter 이미지 향상 요청 중... ({resolution}, {aspectRatio})";

            var resultBytes = await _openRouterImageService.EnhanceAsync(
                _settings.OpenRouterApiKey,
                _settings.OpenRouterModel,
                sourceBytes,
                _settings.OpenRouterPrompt,
                resolution,
                aspectRatio);

            var normalizedResult = NormalizeImageToPng(resultBytes, targetWidth, targetHeight);
            StatusText.Text = $"AI 이미지 향상 완료: {targetWidth} × {targetHeight}";

            var resultWindow = new ResultWindow(sourceBytes, normalizedResult) { Owner = this };
            resultWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"AI 이미지 향상 실패: {ex.Message}";
        }
        finally
        {
            SetProcessing(false);
        }
    }

    private async Task<byte[]> CaptureFrameAsync(int targetWidth, int targetHeight)
    {
        MapWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { type = "setFrameVisible", visible = false }));
        try
        {
            await Task.Delay(80);
            using var stream = new MemoryStream();
            await MapWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            return NormalizeImageToPng(stream.ToArray(), targetWidth, targetHeight);
        }
        finally
        {
            MapWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { type = "setFrameVisible", visible = true }));
        }
    }

    private static byte[] NormalizeImageToPng(byte[] sourceBytes, int targetWidth, int targetHeight)
    {
        using var input = new MemoryStream(sourceBytes);
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        BitmapSource source = decoder.Frames[0];

        var targetRatio = (double)targetWidth / targetHeight;
        var sourceRatio = (double)source.PixelWidth / source.PixelHeight;
        int cropWidth;
        int cropHeight;

        if (sourceRatio > targetRatio)
        {
            cropHeight = source.PixelHeight;
            cropWidth = Math.Max(1, (int)Math.Round(cropHeight * targetRatio));
        }
        else
        {
            cropWidth = source.PixelWidth;
            cropHeight = Math.Max(1, (int)Math.Round(cropWidth / targetRatio));
        }

        var x = Math.Max(0, (source.PixelWidth - cropWidth) / 2);
        var y = Math.Max(0, (source.PixelHeight - cropHeight) / 2);
        source = new CroppedBitmap(source, new Int32Rect(x, y, cropWidth, cropHeight));

        if (source.PixelWidth != targetWidth || source.PixelHeight != targetHeight)
        {
            source = new TransformedBitmap(source, new ScaleTransform(
                (double)targetWidth / source.PixelWidth,
                (double)targetHeight / source.PixelHeight));
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var output = new MemoryStream();
        encoder.Save(output);
        return output.ToArray();
    }

    private static string GetAspectRatio(int width, int height)
    {
        var gcd = GreatestCommonDivisor(width, height);
        return $"{width / gcd}:{height / gcd}";
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            var remainder = a % b;
            a = b;
            b = remainder;
        }
        return Math.Abs(a);
    }

    private bool TryGetOutputSize(out int width, out int height)
    {
        width = 0;
        height = 0;
        if (OutputPresetBox.SelectedItem is not ComboBoxItem item || item.Tag is not string tag) return false;
        var values = tag.Split(',');
        return values.Length == 2 && int.TryParse(values[0], out width) && int.TryParse(values[1], out height);
    }

    private void SetProcessing(bool processing)
    {
        _processing = processing;
        UpdateActionButtons();
    }

    private void UpdateActionButtons()
    {
        CaptureButton.IsEnabled = _webReady && !_processing;
        EnhanceButton.IsEnabled = _webReady && !_processing;
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_settingsService) { Owner = this };
        if (window.ShowDialog() == true)
        {
            _settings = await _settingsService.LoadAsync();
            _webReady = false;
            UpdateActionButtons();
            await MapWebView.CoreWebView2.ExecuteScriptAsync("window.location.reload();");
        }
    }
}
