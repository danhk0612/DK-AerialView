namespace DKAerialView.Models;

public sealed class RoadviewReferenceItem
{
    public string Direction { get; init; } = string.Empty;
    public double BearingDegrees { get; init; }
    public int RequestedDistanceMeters { get; init; }
    public int ActualDistanceMeters { get; init; }
    public long PanoId { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public byte[] ImageBytes { get; init; } = Array.Empty<byte>();
    public bool IsSelected { get; set; } = true;

    public string DisplayLabel => $"{Direction} · 실제 촬영점 약 {ActualDistanceMeters}m";
}
