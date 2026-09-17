using System.Text.Json;
using DKAerialView.Models;
using DKAerialView.Services;

namespace DKAerialView;

public partial class MainWindow
{
    public async Task<IReadOnlyList<RoadviewReferenceItem>> CollectKakaoRoadviewReferencesAsync(
        AerialGenerationOptions options,
        IProgress<RoadviewCollectionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var (targetLatitude, targetLongitude) = await GetCurrentMapCenterAsync(cancellationToken);

        progress?.Report(new RoadviewCollectionProgress
        {
            Phase = "중심점",
            AttemptIndex = 1,
            TotalAttempts = 1,
            CurrentCandidate = "현재 지도 중앙점",
            Message = $"[중심점] 로드뷰 목표 좌표를 현재 지도 중앙으로 갱신했습니다: {targetLatitude:F6}, {targetLongitude:F6}"
        });

        var service = new KakaoRoadviewReferenceService();
        return await service.CollectAsync(
            this,
            _settings.KakaoJavaScriptKey,
            targetLatitude,
            targetLongitude,
            options,
            progress,
            cancellationToken);
    }

    private async Task<(double Latitude, double Longitude)> GetCurrentMapCenterAsync(
        CancellationToken cancellationToken)
    {
        var fallback = (_latitude, _longitude);
        if (!_webReady || MapWebView.CoreWebView2 is null)
            return fallback;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rawJson = await MapWebView.CoreWebView2.ExecuteScriptAsync(
                "window.dkAerialViewGetCenter ? window.dkAerialViewGetCenter() : null");

            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(rawJson) || string.Equals(rawJson, "null", StringComparison.OrdinalIgnoreCase))
                return fallback;

            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("lat", out var latNode) ||
                !root.TryGetProperty("lng", out var lngNode))
                return fallback;

            var latitude = latNode.GetDouble();
            var longitude = lngNode.GetDouble();
            if (!double.IsFinite(latitude) || !double.IsFinite(longitude) ||
                latitude is < -90 or > 90 || longitude is < -180 or > 180)
                return fallback;

            _latitude = latitude;
            _longitude = longitude;
            return (latitude, longitude);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return fallback;
        }
    }
}
