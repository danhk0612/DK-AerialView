using DKAerialView.Models;
using DKAerialView.Services;

namespace DKAerialView;

public partial class MainWindow
{
    public async Task<IReadOnlyList<byte[]>> CollectKakaoRoadviewReferencesAsync(
        AerialGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        var service = new KakaoRoadviewReferenceService();
        return await service.CollectAsync(
            this,
            _settings.KakaoJavaScriptKey,
            _latitude,
            _longitude,
            options,
            cancellationToken);
    }
}
