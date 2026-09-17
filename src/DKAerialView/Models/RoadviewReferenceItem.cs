namespace DKAerialView.Models;

public sealed class RoadviewReferenceItem
{
    public string Direction { get; init; } = string.Empty;
    public double BearingDegrees { get; init; }
    public int RequestedDistanceMeters { get; init; }
    public byte[] ImageBytes { get; init; } = Array.Empty<byte>();
    public bool IsSelected { get; set; } = true;

    public string DisplayLabel => $"{Direction} · 기준점에서 약 {RequestedDistanceMeters}m";
}
