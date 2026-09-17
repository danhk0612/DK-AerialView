namespace DKAerialView.Models;

public sealed record RoadviewCaptureResult(
    long PanoId,
    double Latitude,
    double Longitude,
    double Pan,
    byte[] ImageBytes);
