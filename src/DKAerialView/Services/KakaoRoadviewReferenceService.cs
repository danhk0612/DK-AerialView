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
        IProgress<RoadviewCollectionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!options.UseKakaoRoadviewReferences || string.IsNullOrWhiteSpace(kakaoJavaScriptKey))
            return Array.Empty<RoadviewReferenceItem>();

        var references = new List<RoadviewReferenceItem>();
        var seenPanoIds = new HashSet<long>();
        var bearings = new[] { 0d, 45d, 90d, 135d, 180d, 225d, 270d, 315d };
        var distances = BuildSearchDistances(options.RoadviewDistanceMeters);
        var totalAttempts = distances.Count * bearings.Length;
        var attemptIndex = 0;
        var duplicateCount = 0;
        var failureCount = 0;
        var captureWindow = new RoadviewCaptureWindow(kakaoJavaScriptKey);

        void Report(string currentCandidate, string message, RoadviewReferenceItem? added = null, bool completed = false)
        {
            progress?.Report(new RoadviewCollectionProgress
            {
                AttemptIndex = attemptIndex,
                TotalAttempts = totalAttempts,
                SuccessCount = references.Count,
                DuplicateCount = duplicateCount,
                FailureCount = failureCount,
                CurrentCandidate = currentCandidate,
                Message = message,
                AddedReference = added,
                IsCompleted = completed
            });
        }

        try
        {
            Report("초기화", "카카오 로드뷰 호스트를 초기화하는 중입니다...");
            await captureWindow.InitializeAsync(cancellationToken);
            Report("초기화 완료", "카카오 로드뷰 호스트 준비가 완료되었습니다.");

            foreach (var distance in distances)
            {
                foreach (var bearing in bearings)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (references.Count >= MaxReferences)
                    {
                        Report("완료", $"서로 다른 로드뷰 {references.Count}장을 확보해 수집을 종료합니다.", completed: true);
                        return references;
                    }

                    attemptIndex++;
                    var candidateLabel = $"{DirectionLabel(bearing)} {distance}m";
                    Report(candidateLabel, $"[{attemptIndex}/{totalAttempts}] {candidateLabel} 후보를 탐색 중입니다. 응답이 느리면 수 초 걸릴 수 있습니다.");

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
                        {
                            failureCount++;
                            Report(candidateLabel, $"[실패] {candidateLabel}: 사용 가능한 로드뷰를 찾지 못했습니다.");
                            continue;
                        }

                        if (capture.PanoId != 0 && !seenPanoIds.Add(capture.PanoId))
                        {
                            duplicateCount++;
                            Report(candidateLabel, $"[중복] {candidateLabel}: 이미 확보한 촬영점과 같은 pano라 건너뜁니다.");
                            continue;
                        }

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

                        var item = new RoadviewReferenceItem
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
                        };

                        references.Add(item);
                        Report(candidateLabel, $"[성공] {item.DisplayLabel} 로드뷰를 확보했습니다.", item);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        failureCount++;
                        Report(candidateLabel, $"[시간 초과] {candidateLabel}: 응답이 없어 건너뜁니다.");
                    }
                    catch (TimeoutException)
                    {
                        failureCount++;
                        Report(candidateLabel, $"[시간 초과] {candidateLabel}: 응답이 없어 건너뜁니다.");
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        Report(candidateLabel, $"[오류] {candidateLabel}: {ex.Message}");
                    }
                }
            }

            Report("완료", $"로드뷰 수집 완료: {references.Count}장 확보했습니다.", completed: true);
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
