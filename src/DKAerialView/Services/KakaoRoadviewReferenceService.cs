using System.Windows;
using DKAerialView.Models;

namespace DKAerialView.Services;

public sealed class KakaoRoadviewReferenceService
{
    private const double EarthRadiusMeters = 6378137.0;

    public async Task<IReadOnlyList<RoadviewReferenceItem>> CollectAsync(
        Window owner,
        string kakaoJavaScriptKey,
        double targetLatitude,
        double targetLongitude,
        AerialGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!options.UseKakaoRoadviewReferences || string.IsNullOrWhiteSpace(kakaoJavaScriptKey))
            return Array.Empty<RoadviewReferenceItem>();

        var references = new List<RoadviewReferenceItem>();
        var directions = new[]
        {
            (Bearing: 0d, Label: "북측"),
            (Bearing: 90d, Label: "동측"),
            (Bearing: 180d, Label: "남측"),
            (Bearing: 270d, Label: "서측")
        };

        // Keep the capture window independent from the modal options window / disabled MainWindow.
        // It is fully hidden off-screen and closed as soon as collection finishes.
        var captureWindow = new RoadviewCaptureWindow(kakaoJavaScriptKey);

        try
        {
            await captureWindow.InitializeAsync(cancellationToken);

            foreach (var direction in directions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (lat, lng) = Offset(
                    targetLatitude,
                    targetLongitude,
                    options.RoadviewDistanceMeters,
                    direction.Bearing);

                try
                {
                    var bytes = await captureWindow.CaptureReferenceAsync(
                        targetLatitude,
                        targetLongitude,
                        lat,
                        lng,
                        options.RoadviewSearchRadiusMeters,
                        options.RoadviewTilt,
                        options.RoadviewZoom,
                        cancellationToken);

                    if (bytes is { Length: > 0 })
                    {
                        references.Add(new RoadviewReferenceItem
                        {
                            Direction = direction.Label,
                            BearingDegrees = direction.Bearing,
                            RequestedDistanceMeters = options.RoadviewDistanceMeters,
                            ImageBytes = bytes,
                            IsSelected = true
                        });
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Internal per-direction timeout/cancel: skip only this direction.
                }
                catch (TimeoutException)
                {
                    // Skip only this direction and continue collecting the remaining references.
                }
                catch
                {
                    // A single unavailable/broken panorama should not abort the aerial generation flow.
                }
            }
        }
        finally
        {
            captureWindow.Close();
        }

        return references;
    }

    private static (double Latitude, double Longitude) Offset(double latitude, double longitude, double distanceMeters, double bearingDegrees)
    {
        var angularDistance = distanceMeters / EarthRadiusMeters;
        var bearing = DegreesToRadians(bearingDegrees);
        var lat1 = DegreesToRadians(latitude);
        var lon1 = DegreesToRadians(longitude);

        var lat2 = Math.Asin(
            Math.Sin(lat1) * Math.Cos(angularDistance) +
            Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearing));

        var lon2 = lon1 + Math.Atan2(
            Math.Sin(bearing) * Math.Sin(angularDistance) * Math.Cos(lat1),
            Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2));

        return (RadiansToDegrees(lat2), RadiansToDegrees(lon2));
    }

    private static double DegreesToRadians(double value) => value * Math.PI / 180.0;
    private static double RadiansToDegrees(double value) => value * 180.0 / Math.PI;
}
