namespace DKAerialView.Models;

public sealed class RoadviewCollectionProgress
{
    public int AttemptIndex { get; init; }
    public int TotalAttempts { get; init; }
    public int SuccessCount { get; init; }
    public int DuplicateCount { get; init; }
    public int FailureCount { get; init; }
    public string CurrentCandidate { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public RoadviewReferenceItem? AddedReference { get; init; }
    public bool IsCompleted { get; init; }
}
