using System.Diagnostics;
using CCWaterControllerPlayer.Helpers;
using CCWaterControllerPlayer.Models;

namespace CCWaterControllerPlayer.Services;

public enum RecordingState
{
    Idle,
    Buffering,
    Recording,
    PostRecording
}

public class RecordingService : IDisposable
{
    private RingBuffer<ControllerSnapshot> _ringBuffer;
    private readonly SettingsService _settingsService;
    private RecordingState _state = RecordingState.Idle;
    private long _triggerStartTicks;
    private long _triggerEndTicks;
    private long _postRecordingDeadlineTicks;
    private List<ControllerSnapshot> _currentRecording = new();
    private readonly object _lock = new();

    public event EventHandler<TrackRecord>? RecordingCompleted;
    public event EventHandler<RecordingState>? StateChanged;

    public RecordingState State => _state;
    private AppSettings Settings => _settingsService.Settings;

    public RecordingService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _ringBuffer = new RingBuffer<ControllerSnapshot>(ComputeBufferSize(settingsService.Settings));
    }

    public void UpdateSettings(AppSettings settings)
    {
        _ringBuffer = new RingBuffer<ControllerSnapshot>(ComputeBufferSize(settings));
    }

    public void UpdateBufferSize(int samplingRateHz)
    {
        int bufferSize = Math.Clamp(samplingRateHz * Settings.RingBufferSeconds, 1000, 10000);
        _ringBuffer = new RingBuffer<ControllerSnapshot>(bufferSize);
    }

    private static int ComputeBufferSize(AppSettings settings)
    {
        return Math.Clamp(settings.SamplingRateHz * settings.RingBufferSeconds, 1000, 10000);
    }

    public void OnInputReceived(ControllerSnapshot snapshot)
    {
        _ringBuffer.Write(snapshot);

        lock (_lock)
        {
            switch (_state)
            {
                case RecordingState.Idle:
                case RecordingState.Buffering:
                    break;

                case RecordingState.Recording:
                    _currentRecording.Add(snapshot);
                    break;

                case RecordingState.PostRecording:
                    _currentRecording.Add(snapshot);
                    if (snapshot.TimestampTicks >= _postRecordingDeadlineTicks)
                    {
                        FinalizeRecording();
                    }
                    break;
            }
        }
    }

    public void OnTriggerPressed(long timestampTicks)
    {
        lock (_lock)
        {
            switch (_state)
            {
                case RecordingState.Idle:
                case RecordingState.Buffering:
                    StartRecording(timestampTicks);
                    break;

                case RecordingState.PostRecording:
                    if (Settings.TriggerConfig.MergeEnabled)
                    {
                        _state = RecordingState.Recording;
                        StateChanged?.Invoke(this, _state);
                    }
                    else
                    {
                        FinalizeRecording();
                        StartRecording(timestampTicks);
                    }
                    break;
            }
        }
    }

    public void OnTriggerReleased(long timestampTicks)
    {
        lock (_lock)
        {
            if (_state == RecordingState.Recording)
            {
                _triggerEndTicks = timestampTicks;
                long postDurationTicks = (long)(Settings.TriggerConfig.PostTriggerMs * TimeSpan.TicksPerMillisecond);
                _postRecordingDeadlineTicks = timestampTicks + postDurationTicks;
                _state = RecordingState.PostRecording;
                StateChanged?.Invoke(this, _state);
            }
        }
    }

    private void StartRecording(long timestampTicks)
    {
        _triggerStartTicks = timestampTicks;
        _currentRecording = new List<ControllerSnapshot>();

        long preDurationTicks = (long)(Settings.TriggerConfig.PreTriggerMs * TimeSpan.TicksPerMillisecond);
        long preStartTicks = timestampTicks - preDurationTicks;

        var preData = _ringBuffer.ReadRange(preStartTicks, timestampTicks, s => s.TimestampTicks);
        _currentRecording.AddRange(preData);

        _state = RecordingState.Recording;
        StateChanged?.Invoke(this, _state);
    }

    private void FinalizeRecording()
    {
        var record = new TrackRecord
        {
            CreatedAt = DateTime.Now,
            TriggerStartTicks = _triggerStartTicks,
            TriggerEndTicks = _triggerEndTicks,
            SampleCount = _currentRecording.Count,
            SamplingRateHz = Settings.SamplingRateHz,
            TrackedStick = Settings.DefaultTrackedStick,

            Snapshots = new List<ControllerSnapshot>(_currentRecording)
        };

        _currentRecording.Clear();
        _state = RecordingState.Idle;
        StateChanged?.Invoke(this, _state);
        RecordingCompleted?.Invoke(this, record);
    }

    public bool IsTriggerActive(ControllerSnapshot snapshot)
    {
        var config = Settings.TriggerConfig;

        return config.Mode switch
        {
            TriggerMode.SingleButton => IsSingleButtonActive(snapshot, config),
            TriggerMode.ComboButtons => IsComboActive(snapshot, config),
            TriggerMode.ThresholdBased => IsThresholdActive(snapshot, config),
            _ => false
        };
    }

    private static bool IsSingleButtonActive(ControllerSnapshot snapshot, RecordingTriggerConfig config)
    {
        if (config.TriggerButton == GamepadButton.RightTrigger)
            return snapshot.Triggers.Right >= config.TriggerThreshold;
        if (config.TriggerButton == GamepadButton.LeftTrigger)
            return snapshot.Triggers.Left >= config.TriggerThreshold;

        return (snapshot.Buttons & (uint)config.TriggerButton) != 0;
    }

    private static bool IsComboActive(ControllerSnapshot snapshot, RecordingTriggerConfig config)
    {
        if (config.ComboButtons == null || config.ComboButtons.Length == 0)
            return false;

        foreach (var button in config.ComboButtons)
        {
            if (button == GamepadButton.RightTrigger)
            {
                if (snapshot.Triggers.Right < config.TriggerThreshold) return false;
            }
            else if (button == GamepadButton.LeftTrigger)
            {
                if (snapshot.Triggers.Left < config.TriggerThreshold) return false;
            }
            else
            {
                if ((snapshot.Buttons & (uint)button) == 0) return false;
            }
        }
        return true;
    }

    private static bool IsThresholdActive(ControllerSnapshot snapshot, RecordingTriggerConfig config)
    {
        return snapshot.RightStick.Magnitude >= config.TriggerThreshold ||
               snapshot.LeftStick.Magnitude >= config.TriggerThreshold;
    }

    public void Dispose()
    {
        _currentRecording.Clear();
    }
}
