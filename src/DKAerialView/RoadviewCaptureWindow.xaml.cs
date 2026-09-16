using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace DKAerialView;

public partial class RoadviewCaptureWindow : Window
{
    private const string HostName = "app.dk-aerialview.local";
    private readonly string _kakaoJavaScriptKey;
    private readonly Dictionary<string, TaskCompletionSource<RoadviewReadyMessage>> _pending = new();
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
        if (string.IsNullOrWhiteSpace(requestId) || !_pending.TryGetValue(requestId, out var tcs)) return;

        if (type == "roadviewReady")
        {
            var panoId = root.TryGetProperty("panoId", out var panoNode) ? panoNode.GetInt64() : 0;
            var pan = root.TryGetProperty("pan", out var panNode) ? panNode.GetDouble() : 0;
            var lat = root.TryGetProperty("lat", out var latNode) ? latNode.GetDouble() : 0;
            var lng = root.TryGetProperty("lng", out var lngNode) ? lngNode.GetDouble() : 0;
            tcs.TrySetResult(new RoadviewReadyMessage(true, panoId, pan, lat, lng, null));
        }
        else if (type == "roadviewNotFound")
        {
            tcs.TrySetResult(new RoadviewReadyMessage(false, 0, 0, 0, 0, null));
        }
        else if (type == "roadviewError")
        {
            var message = root.TryGetProperty("message", out var messageNode)
                ? messageNode.GetString()
                : "Kakao Roadview 오류";
            tcs.TrySetResult(new RoadviewReadyMessage(false, 0, 0, 0, 0, message));
        }
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (!IsVisible) Show();
        using var registration = cancellationToken.Register(() => _hostReady.TrySetCanceled(cancellationToken));
        await _hostReady.Task;
    }

    public async Task<byte[]?> CaptureReferenceAsync(
        double targetLat,
        double targetLng,
        double searchLat,
        double searchLng,
        int radiusMeters,
        int tilt,
        int zoom,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        var requestId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<RoadviewReadyMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        try
        {
            RoadviewWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new
            {
                type = "prepareRoadview",
                requestId,
                targetLat,
                targetLng,
                searchLat,
                searchLng,
                radius = radiusMeters,
                tilt,
                zoom
            }));

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(12));
            using var registration = timeout.Token.Register(() => tcs.TrySetCanceled(timeout.Token));
            var ready = await tcs.Task;
            if (!ready.Found || !string.IsNullOrWhiteSpace(ready.Error)) return null;

            await Task.Delay(250, cancellationToken);
            using var stream = new MemoryStream();
            await RoadviewWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            return stream.ToArray();
        }
        finally
        {
            _pending.Remove(requestId);
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
