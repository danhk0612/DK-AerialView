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
        var service = new KakaoRoadviewReferenceService();
        return await service.CollectAsync(
            this,
            _settings.KakaoJavaScriptKey,
            _latitude,
            _longitude,
            options,
            progress,
            cancellationToken);
    }
}
