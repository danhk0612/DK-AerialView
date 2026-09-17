using System.Windows;
using DKAerialView.Models;

namespace DKAerialView.Services;

public sealed class KakaoRoadviewReferenceService
{
    private const double EarthRadiusMeters = 6378137.0;
    private const int MaxReferences = 4;
    private const int ProbeConcurrency = 4;
    private const int PreferredLocalProbeRadiusMeters = 35;

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
        var candidates = new Dictionary<long, ProbeCandidate>();
        var duplicateCount = 0;
        var failureCount = 0;
        var captureWindow = new RoadviewCaptureWindow(kakaoJavaScriptKey);
        var probeStages = BuildProbeStages(options.RoadviewDistanceMeters, options.RoadviewSearchRadiusMeters);
        var totalPlannedProbes = probeStages.Sum(stage => stage.Bearings.Count);
        var completedProbes = 0;

        void Report(
            string phase,
            int attemptIndex,
            int totalAttempts,
            string currentCandidate,
            string message,
            RoadviewReferenceItem? added = null,
            bool completed = false)
        {
            progress?.Report(new RoadviewCollectionProgress
            {
                Phase = phase,
                AttemptIndex = attemptIndex,
                TotalAttempts = totalAttempts,
                CandidateCount = candidates.Count,
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
            Report("초기화", 0, 1, "초기화", "카카오 로드뷰 호스트를 초기화하는 중입니다...");
            await captureWindow.InitializeAsync(cancellationToken);
            Report("초기화", 1, 1, "초기화 완료", "카카오 로드뷰 호스트 준비가 완료되었습니다.");

            using var probeGate = new SemaphoreSlim(ProbeConcurrency);

            for (var stageIndex = 0; stageIndex < probeStages.Count; stageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (candidates.Count >= MaxReferences)
                    break;

                var stage = probeStages[stageIndex];
                Report(
                    "탐색",
                    completedProbes,
                    totalPlannedProbes,
                    $"탐색 단계 {stageIndex + 1}/{probeStages.Count}",
                    $"{stage.Description} · 검색 반경 {stage.RadiusMeters}m · 고유 pano 후보 {candidates.Count}개");

                async Task<ProbeOutcome> ProbeAsync(double bearing)
                {
                    await probeGate.WaitAsync(cancellationToken);
                    try
                    {
                        var (searchLat, searchLng) = Offset(
                            targetLatitude,
                            targetLongitude,
                            stage.DistanceMeters,
                            bearing);
                        try
                        {
                            var panoId = await captureWindow.ProbeNearestPanoIdAsync(
                                searchLat,
                                searchLng,
                                stage.RadiusMeters,
                                cancellationToken);
                            return new ProbeOutcome(
                                bearing,
                                stage.DistanceMeters,
                                stage.RadiusMeters,
                                searchLat,
                                searchLng,
                                panoId,
                                null);
                        }
                        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                        {
                            return new ProbeOutcome(
                                bearing, stage.DistanceMeters, stage.RadiusMeters,
                                searchLat, searchLng, null, "시간 초과");
                        }
                        catch (Exception ex)
                        {
                            return new ProbeOutcome(
                                bearing, stage.DistanceMeters, stage.RadiusMeters,
                                searchLat, searchLng, null, ex.Message);
                        }
                    }
                    finally
                    {
                        probeGate.Release();
                    }
                }

                var pending = stage.Bearings.Select(ProbeAsync).ToList();
                while (pending.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var completedTask = await Task.WhenAny(pending);
                    pending.Remove(completedTask);
                    var outcome = await completedTask;
                    completedProbes++;

                    var label = $"{DirectionLabel(outcome.Bearing)} {outcome.DistanceMeters}m / 반경 {outcome.RadiusMeters}m";
                    if (!string.IsNullOrWhiteSpace(outcome.Error))
                    {
                        failureCount++;
                        Report(
                            "탐색",
                            completedProbes,
                            totalPlannedProbes,
                            label,
                            $"[실패] {label}: {outcome.Error}");
                        continue;
                    }

                    if (outcome.PanoId is null or 0)
                    {
                        failureCount++;
                        Report(
                            "탐색",
                            completedProbes,
                            totalPlannedProbes,
                            label,
                            $"[없음] {label}: 주변 pano 없음");
                        continue;
                    }

                    if (candidates.ContainsKey(outcome.PanoId.Value))
                    {
                        duplicateCount++;
                        Report(
                            "탐색",
                            completedProbes,
                            totalPlannedProbes,
                            label,
                            $"[중복] {label}: pano {outcome.PanoId.Value}는 이미 후보에 있음");
                        continue;
                    }

                    candidates.Add(
                        outcome.PanoId.Value,
                        new ProbeCandidate(
                            outcome.PanoId.Value,
                            outcome.SearchLatitude,
                            outcome.SearchLongitude,
                            outcome.Bearing,
                            outcome.DistanceMeters));

                    Report(
                        "탐색",
                        completedProbes,
                        totalPlannedProbes,
                        label,
                        $"[후보] pano {outcome.PanoId.Value} 추가 · 고유 후보 {candidates.Count}개");
                }

                if (candidates.Count >= MaxReferences)
                {
                    Report(
                        "탐색",
                        completedProbes,
                        totalPlannedProbes,
                        $"탐색 단계 {stageIndex + 1} 완료",
                        $"고유 pano 후보 {candidates.Count}개를 확보해 남은 탐색 단계를 생략합니다.");
                    break;
                }
            }

            if (candidates.Count == 0)
            {
                Report("완료", 1, 1, "완료", "사용 가능한 고유 pano 후보를 찾지 못했습니다.", completed: true);
                return references;
            }

            var captureOrder = RankCandidates(candidates.Values, options.RoadviewDistanceMeters);
            Report(
                "캡처",
                0,
                Math.Min(captureOrder.Count, MaxReferences),
                "캡처 준비",
                $"고유 pano 후보 {candidates.Count}개 중 방향 분산이 좋은 후보부터 실제 로드뷰를 렌더링합니다.");

            var captureAttempt = 0;
            foreach (var candidate in captureOrder)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (references.Count >= MaxReferences)
                    break;

                captureAttempt++;
                var candidateLabel = $"{DirectionLabel(candidate.BearingDegrees)} {candidate.RequestedDistanceMeters}m";
                Report(
                    "캡처",
                    Math.Min(captureAttempt, MaxReferences),
                    MaxReferences,
                    candidateLabel,
                    $"[캡처] pano {candidate.PanoId}를 실제 로드뷰로 렌더링 중입니다.");

                try
                {
                    var capture = await captureWindow.CapturePanoAsync(
                        candidate.PanoId,
                        targetLatitude,
                        targetLongitude,
                        candidate.SearchLatitude,
                        candidate.SearchLongitude,
                        options.RoadviewTilt,
                        options.RoadviewZoom,
                        cancellationToken);

                    if (capture is null || capture.ImageBytes.Length == 0)
                    {
                        failureCount++;
                        Report(
                            "캡처",
                            Math.Min(captureAttempt, MaxReferences),
                            MaxReferences,
                            candidateLabel,
                            $"[캡처 실패] pano {candidate.PanoId}: 다음 후보를 시도합니다.");
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
                        RequestedDistanceMeters = candidate.RequestedDistanceMeters,
                        ActualDistanceMeters = actualDistance,
                        PanoId = capture.PanoId,
                        Latitude = capture.Latitude,
                        Longitude = capture.Longitude,
                        ImageBytes = capture.ImageBytes,
                        IsSelected = true
                    };

                    references.Add(item);
                    Report(
                        "캡처",
                        Math.Min(captureAttempt, MaxReferences),
                        MaxReferences,
                        candidateLabel,
                        $"[성공] {item.DisplayLabel} 로드뷰를 확보했습니다.",
                        item);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    failureCount++;
                    Report(
                        "캡처",
                        Math.Min(captureAttempt, MaxReferences),
                        MaxReferences,
                        candidateLabel,
                        $"[시간 초과] pano {candidate.PanoId}: 다음 후보를 시도합니다.");
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Report(
                        "캡처",
                        Math.Min(captureAttempt, MaxReferences),
                        MaxReferences,
                        candidateLabel,
                        $"[오류] pano {candidate.PanoId}: {ex.Message}");
                }
            }

            Report(
                "완료",
                1,
                1,
                "완료",
                $"로드뷰 수집 완료: 고유 pano 후보 {candidates.Count}개 탐색, 실제 이미지 {references.Count}장 확보.",
                completed: true);
        }
        finally
        {
            captureWindow.Close();
        }

        return references;
    }

    private static IReadOnlyList<ProbeStage> BuildProbeStages(int preferredDistance, int maxSearchRadius)
    {
        var localRadius = Math.Max(10, Math.Min(maxSearchRadius, PreferredLocalProbeRadiusMeters));
        var cardinalAndDiagonal = new[] { 0d, 45d, 90d, 135d, 180d, 225d, 270d, 315d };
        var offsetBearings = new[] { 22.5d, 67.5d, 112.5d, 157.5d, 202.5d, 247.5d, 292.5d, 337.5d };
        var stages = new List<ProbeStage>
        {
            new(preferredDistance, localRadius, cardinalAndDiagonal, $"우선 거리 {preferredDistance}m의 기본 8방향"),
            new(preferredDistance, localRadius, offsetBearings, $"우선 거리 {preferredDistance}m의 보조 8방향")
        };

        foreach (var distance in new[] { 50, 100, 150, 200 }.Where(value => value != preferredDistance))
        {
            stages.Add(new ProbeStage(
                distance,
                localRadius,
                cardinalAndDiagonal,
                $"추가 거리 {distance}m의 8방향"));
        }

        if (maxSearchRadius > localRadius)
        {
            stages.Add(new ProbeStage(
                preferredDistance,
                maxSearchRadius,
                cardinalAndDiagonal,
                $"마지막 확장 검색 (최대 반경 {maxSearchRadius}m)"));
        }

        return stages;
    }

    private static IReadOnlyList<ProbeCandidate> RankCandidates(
        IEnumerable<ProbeCandidate> source,
        int preferredDistance)
    {
        var remaining = source
            .OrderBy(candidate => Math.Abs(candidate.RequestedDistanceMeters - preferredDistance))
            .ThenBy(candidate => candidate.RequestedDistanceMeters)
            .ToList();
        var ranked = new List<ProbeCandidate>();

        foreach (var targetBearing in new[] { 0d, 90d, 180d, 270d })
        {
            if (remaining.Count == 0) break;
            var best = remaining
                .OrderBy(candidate => AngularDistance(candidate.BearingDegrees, targetBearing))
                .ThenBy(candidate => Math.Abs(candidate.RequestedDistanceMeters - preferredDistance))
                .First();
            ranked.Add(best);
            remaining.Remove(best);
        }

        ranked.AddRange(remaining
            .OrderBy(candidate => Math.Abs(candidate.RequestedDistanceMeters - preferredDistance))
            .ThenBy(candidate => candidate.BearingDegrees));
        return ranked;
    }

    private static double AngularDistance(double left, double right)
    {
        var difference = Math.Abs((((left - right) % 360) + 360) % 360);
        return Math.Min(difference, 360 - difference);
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

    private static (double Latitude, double Longitude) Offset(
        double latitude,
        double longitude,
        double distanceMeters,
        double bearingDegrees)
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

    private sealed record ProbeOutcome(
        double Bearing,
        int DistanceMeters,
        int RadiusMeters,
        double SearchLatitude,
        double SearchLongitude,
        long? PanoId,
        string? Error);

    private sealed record ProbeCandidate(
        long PanoId,
        double SearchLatitude,
        double SearchLongitude,
        double BearingDegrees,
        int RequestedDistanceMeters);

    private sealed record ProbeStage(
        int DistanceMeters,
        int RadiusMeters,
        IReadOnlyList<double> Bearings,
        string Description);
}
