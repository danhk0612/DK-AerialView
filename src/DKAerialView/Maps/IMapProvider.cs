namespace DKAerialView.Maps;

public interface IMapProvider
{
    string Id { get; }
    string DisplayName { get; }
    MapCapabilities Capabilities { get; }
}

public sealed record MapCapabilities(
    bool SupportsSatellite,
    bool SupportsTilt,
    bool SupportsHeading,
    bool SupportsAddressSearch);
