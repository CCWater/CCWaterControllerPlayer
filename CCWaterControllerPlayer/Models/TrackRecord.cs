namespace CCWaterControllerPlayer.Models;

public class TrackRecord
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long TriggerStartTicks { get; set; }
    public long TriggerEndTicks { get; set; }
    public int SampleCount { get; set; }
    public int SamplingRateHz { get; set; }
    public StickSide TrackedStick { get; set; }
    public string? Notes { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Temporary;
    public List<ControllerSnapshot> Snapshots { get; set; } = new();

    public string DisplayName => string.IsNullOrWhiteSpace(Name) 
        ? $"{CreatedAt:MM/dd HH:mm:ss}" 
        : Name;
}
