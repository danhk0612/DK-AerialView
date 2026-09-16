using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DKAerialView.Models;
using DKAerialView.Services;
using Microsoft.Web.WebView2.Core;

namespace DKAerialView;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings = new();
    private bool _webReady;
    private bool _syncingCamera;
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
                StatusText.Text = "Google 지도 준비됨";
                await SendCameraAsync();
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

    private async Task SendInitializeAsync()
    {
        var command = new { type = "initialize", provider = "google", apiKey = _settings.GoogleMapsApiKey };
        MapWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(command));
        await Task.CompletedTask;
    }

    private async Task SendCameraAsync()
    {
        if (!_webReady || _syncingCamera) return;
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
        await Task.CompletedTask;
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await SearchAddressAsync();

    private async void AddressBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await SearchAddressAsync();
    }

    private async Task SearchAddressAsync()
    {
        if (!_webReady || string.IsNullOrWhiteSpace(AddressBox.Text)) return;
        var command = new { type = "searchAddress", address = AddressBox.Text.Trim() };
        MapWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(command));
        StatusText.Text = "주소 검색 중...";
        await Task.CompletedTask;
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

    private void ProviderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) StatusText.Text = "현재 1차 버전은 Google Provider만 활성화되어 있습니다.";
    }

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_settingsService) { Owner = this };
        if (window.ShowDialog() == true)
        {
            _settings = await _settingsService.LoadAsync();
            _webReady = false;
            await MapWebView.CoreWebView2.ExecuteScriptAsync("window.location.reload();");
        }
    }
}
