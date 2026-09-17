using System.Windows;
using DKAerialView.Models;

namespace DKAerialView.Services;

public sealed class KakaoRoadviewReferenceService
{
    private const double EarthRadiusMeters = 6378137.0;
    private const int MaxReferences = 4;

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
        var seenPanoIds = new HashSet<long>();
        var bearings = new[] { 0d, 45d, 90d, 135d, 180d, 225d, 270d, 315d };
        var distances = BuildSearchDistances(options.RoadviewDistanceMeters);
        var captureWindow = new RoadviewCaptureWindow(kakaoJavaScriptKey);

        try
        {
            await captureWindow.InitializeAsync(cancellationToken);

            foreach (var distance in distances)
            {
                foreach (var bearing in bearings)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (references.Count >= MaxReferences)
                        return references;

                    var (searchLat, searchLng) = Offset(
                        targetLatitude,
                        targetLongitude,
                        distance,
                        bearing);

                    try
                    {
                        var capture = await captureWindow.CaptureReferenceAsync(
                            targetLatitude,
                            targetLongitude,
                            searchLat,
                            searchLng,
                            options.RoadviewSearchRadiusMeters,
                            options.RoadviewTilt,
                            options.RoadviewZoom,
                            cancellationToken);

                        if (capture is null || capture.ImageBytes.Length == 0)
                            continue;

                        if (capture.PanoId != 0 && !seenPanoIds.Add(capture.PanoId))
                            continue;

                        var actualBearing = InitialBearingDegrees(
                            targetLatitude,
                            targetLongitude,
                            capture.Latitude,
                            capture.Longitude);
                        var actualDistance = (int)Math.Round(DistanceMeters(
                            targetLatitude,
                            targetLongitude,
                            capture.Latitude,
                            capture.Longitude));

                        references.Add(new RoadviewReferenceItem
                        {
                            Direction = DirectionLabel(actualBearing),
                            BearingDegrees = actualBearing,
                            RequestedDistanceMeters = distance,
                            ActualDistanceMeters = actualDistance,
                            PanoId = capture.PanoId,
                            Latitude = capture.Latitude,
                            Longitude = capture.Longitude,
                            ImageBytes = capture.ImageBytes,
                            IsSelected = true
                        });
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                    }
                    catch (TimeoutException)
                    {
                    }
                    catch
                    {
                    }
                }
            }
        }
        finally
        {
            captureWindow.Close();
        }

        return references;
    }

    private static IReadOnlyList<int> BuildSearchDistances(int preferredDistance)
    {
        var candidates = new[] { preferredDistance, 50, 100, 150, 200 };
        return candidates
            .Where(value => value > 0)
            .Distinct()
            .ToArray();
    }

    private static string DirectionLabel(double bearing)
    {
        var index = (int)Math.Round((((bearing % 360) + 360) % 360) / 45.0) % 8;
        return index switch
        {
            0 => "북측",
            1 => "북동측",
            2 => "동측",
            3 => "남동측",
            4 => "남측",
            5 => "남서측",
            6 => "서측",
            _ => "북서측"
        };
    }

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var phi1 = DegreesToRadians(lat1);
        var phi2 = DegreesToRadians(lat2);
        var deltaPhi = DegreesToRadians(lat2 - lat1);
        var deltaLambda = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double InitialBearingDegrees(double lat1, double lon1, double lat2, double lon2)
    {
        var phi1 = DegreesToRadians(lat1);
        var phi2 = DegreesToRadians(lat2);
        var deltaLambda = DegreesToRadians(lon2 - lon1);
        var y = Math.Sin(deltaLambda) * Math.Cos(phi2);
        var x = Math.Cos(phi1) * Math.Sin(phi2) -
                Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(deltaLambda);
        var degrees = RadiansToDegrees(Math.Atan2(y, x));
        return (degrees + 360) % 360;
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
