namespace DKAerialView.Models;

public sealed class PlaceSearchItem
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string RoadAddress { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string PlaceUrl { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public string DisplayAddress => !string.IsNullOrWhiteSpace(RoadAddress) ? RoadAddress : Address;
}
