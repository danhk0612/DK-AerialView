namespace DKAerialView.Models;

public sealed class AppSettings
{
    public string GoogleMapsApiKey { get; set; } = string.Empty;
    public string NaverClientId { get; set; } = string.Empty;
    public string KakaoJavaScriptKey { get; set; } = string.Empty;
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string OpenRouterModel { get; set; } = "bytedance-seed/seedream-4.5";
    public string OpenRouterPrompt { get; set; } = "Preserve the exact buildings, roads, boundaries, terrain layout, and geometry of the reference image. Improve clarity, texture detail, lighting, and overall aerial visualization quality. Do not add, remove, relocate, or redesign structures.";
    public string OpenRouterAerialPrompt { get; set; } = "Use the provided aerial image as the structural reference. Preserve the real layout of buildings, roads, site boundaries, open spaces, terrain relationships, and major structures. Do not invent new buildings, remove existing structures, or significantly relocate roads or boundaries.";
    public string DefaultMapProvider { get; set; } = "google3d";
    public string DefaultOutputPreset { get; set; } = "1920x1080";
}
