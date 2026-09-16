namespace DKAerialView.Models;

public sealed class AppSettings
{
    public string GoogleMapsApiKey { get; set; } = string.Empty;
    public string NaverClientId { get; set; } = string.Empty;
    public string KakaoJavaScriptKey { get; set; } = string.Empty;
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string OpenRouterModel { get; set; } = "bytedance-seed/seedream-4.5";
    public string DefaultMapProvider { get; set; } = "Google";
    public string DefaultOutputPreset { get; set; } = "1920x1080";
}
