namespace DKAerialView.Maps;

public sealed class GoogleMapProvider : IMapProvider
{
    public string Id => "google";
    public string DisplayName => "Google";
    public MapCapabilities Capabilities { get; } = new(true, true, true, true);
}
