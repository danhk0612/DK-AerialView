namespace DKAerialView.Models;

public sealed class AppSettings
{
    public string GoogleMapsApiKey { get; set; } = string.Empty;
    public string NaverClientId { get; set; } = string.Empty;
    public string KakaoJavaScriptKey { get; set; } = string.Empty;
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string OpenRouterModel { get; set; } = "bytedance-seed/seedream-4.5";
    public string OpenRouterPrompt { get; set; } = "Preserve the exact buildings, roads, boundaries, terrain layout, and geometry of the reference image. Improve clarity, texture detail, lighting, and overall aerial visualization quality. Do not add, remove, relocate, or redesign structures.";
    public string OpenRouterAerialPrompt { get; set; } = "Generate a NEW perspective reconstruction of the target site, not a simple enhancement of the top-down aerial image. The output must be a visibly oblique bird's-eye view with building facades and vertical sides clearly visible. Preserve the real building footprints, roads, boundaries, open spaces, and relative positions from the aerial reference while changing the camera projection away from nadir/top-down. Use any Roadview references to infer real building height, facade appearance, roof form, materials, and vertical proportions. Do not invent, remove, or relocate major structures.";
    public string DefaultMapProvider { get; set; } = "kakao";
    public string DefaultOutputPreset { get; set; } = "1920x1080";
}
