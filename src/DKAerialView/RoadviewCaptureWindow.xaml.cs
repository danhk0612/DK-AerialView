using System.IO;
using System.Text.Json;
using System.Windows;
using DKAerialView.Models;
using Microsoft.Web.WebView2.Core;

namespace DKAerialView;

public partial class RoadviewCaptureWindow : Window
{
    private const string HostName = "app.dk-aerialview.local";
    private readonly string _kakaoJavaScriptKey;
    private readonly Dictionary<string, TaskCompletionSource<long?>> _probePending = new();
    private readonly Dictionary<string, TaskCompletionSource<RoadviewReadyMessage>> _renderPending = new();
    private readonly TaskCompletionSource _hostReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public RoadviewCaptureWindow(string kakaoJavaScriptKey)
    {
        InitializeComponent();
        _kakaoJavaScriptKey = kakaoJavaScriptKey;
        Loaded += RoadviewCaptureWindow_Loaded;
    }

    private async void RoadviewCaptureWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await RoadviewWebView.EnsureCoreWebView2Async();
        RoadviewWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        var hostFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "RoadviewHost");
        RoadviewWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            HostName,
            hostFolder,
            CoreWebView2HostResourceAccessKind.Allow);
        RoadviewWebView.Source = new Uri($"https://{HostName}/index.html");
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        using var doc = JsonDocument.Parse(e.WebMessageAsJson);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeNode)) return;
        var type = typeNode.GetString();

        if (type == "roadviewHostLoaded")
        {
            RoadviewWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "initializeRoadview",
                kakaoJavaScriptKey = _kakaoJavaScriptKey
            }));
            return;
        }

        if (type == "roadviewHostReady")
        {
            _hostReady.TrySetResult();
            return;
        }

        if (!root.TryGetProperty("requestId", out var requestIdNode)) return;
        var requestId = requestIdNode.GetString();
        if (string.IsNullOrWhiteSpace(requestId)) return;

        if (type == "panoProbeResult")
        {
            if (!_probePending.TryGetValue(requestId, out var probeTcs)) return;
            var panoId = root.TryGetProperty("panoId", out var panoNode) ? panoNode.GetInt64() : 0;
            probeTcs.TrySetResult(panoId == 0 ? null : panoId);
            return;
        }

        if (!_renderPending.TryGetValue(requestId, out var renderTcs))
        {
            if (type == "roadviewError" && _probePending.TryGetValue(requestId, out var probeErrorTcs))
                probeErrorTcs.TrySetResult(null);
            return;
        }

        if (type == "roadviewReady")
        {
            var panoId = root.TryGetProperty("panoId", out var panoNode) ? panoNode.GetInt64() : 0;
            var pan = root.TryGetProperty("pan", out var panNode) ? panNode.GetDouble() : 0;
            var lat = root.TryGetProperty("lat", out var latNode) ? latNode.GetDouble() : 0;
            var lng = root.TryGetProperty("lng", out var lngNode) ? lngNode.GetDouble() : 0;
            renderTcs.TrySetResult(new RoadviewReadyMessage(true, panoId, pan, lat, lng, null));
        }
        else if (type == "roadviewNotFound")
        {
            renderTcs.TrySetResult(new RoadviewReadyMessage(false, 0, 0, 0, 0, null));
        }
        else if (type == "roadviewError")
        {
            var message = root.TryGetProperty("message", out var messageNode)
                ? messageNode.GetString()
                : "Kakao Roadview 오류";
            renderTcs.TrySetResult(new RoadviewReadyMessage(false, 0, 0, 0, 0, message));
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!IsVisible) Show();

        var delayTask = Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
        var completed = await Task.WhenAny(_hostReady.Task, delayTask);
        cancellationToken.ThrowIfCancellationRequested();

        if (completed != _hostReady.Task)
            throw new TimeoutException("Kakao Roadview 호스트 초기화 시간이 초과되었습니다.");

        await _hostReady.Task;
    }

    public async Task<long?> ProbeNearestPanoIdAsync(
        double searchLat,
        double searchLng,
        int radiusMeters,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        var requestId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<long?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _probePending[requestId] = tcs;

        try
        {
            RoadviewWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "probeRoadview",
                requestId,
                searchLat,
                searchLng,
                radius = radiusMeters
            }));

            var delayTask = Task.Delay(TimeSpan.FromSeconds(6), cancellationToken);
            var completed = await Task.WhenAny(tcs.Task, delayTask);
            cancellationToken.ThrowIfCancellationRequested();

            return completed == tcs.Task ? await tcs.Task : null;
        }
        finally
        {
            _probePending.Remove(requestId);
        }
    }

    public async Task<RoadviewCaptureResult?> CapturePanoAsync(
        long panoId,
        double targetLat,
        double targetLng,
        double searchLat,
        double searchLng,
        int tilt,
        int zoom,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        var requestId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<RoadviewReadyMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _renderPending[requestId] = tcs;

        try
        {
            RoadviewWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "renderRoadview",
                requestId,
                panoId,
                targetLat,
                targetLng,
                searchLat,
                searchLng,
                tilt,
                zoom
            }));

            var delayTask = Task.Delay(TimeSpan.FromSeconds(12), cancellationToken);
            var completed = await Task.WhenAny(tcs.Task, delayTask);
            cancellationToken.ThrowIfCancellationRequested();

            if (completed != tcs.Task)
                return null;

            var ready = await tcs.Task;
            if (!ready.Found || !string.IsNullOrWhiteSpace(ready.Error)) return null;

            await Task.Delay(300, cancellationToken);
            using var stream = new MemoryStream();
            await RoadviewWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            return new RoadviewCaptureResult(
                ready.PanoId,
                ready.Latitude,
                ready.Longitude,
                ready.Pan,
                stream.ToArray());
        }
        finally
        {
            _renderPending.Remove(requestId);
        }
    }

    private sealed record RoadviewReadyMessage(
        bool Found,
        long PanoId,
        double Pan,
        double Latitude,
        double Longitude,
        string? Error);
}
