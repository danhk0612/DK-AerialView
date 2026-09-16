namespace DKAerialView.Models;

public enum AerialViewAnglePreset
{
    Automatic,
    LowOblique,
    StandardOblique,
    HighOblique
}

public enum AerialDirectionPreset
{
    Automatic,
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest
}

public enum StructurePreservationLevel
{
    High,
    Medium
}

public enum AerialRenderStyle
{
    Realistic,
    Architectural
}

public sealed class AerialGenerationOptions
{
    public AerialViewAnglePreset ViewAngle { get; init; } = AerialViewAnglePreset.StandardOblique;
    public AerialDirectionPreset Direction { get; init; } = AerialDirectionPreset.Automatic;
    public StructurePreservationLevel StructurePreservation { get; init; } = StructurePreservationLevel.High;
    public AerialRenderStyle RenderStyle { get; init; } = AerialRenderStyle.Realistic;
    public int TargetWidth { get; init; } = 1920;
    public int TargetHeight { get; init; } = 1080;
}
