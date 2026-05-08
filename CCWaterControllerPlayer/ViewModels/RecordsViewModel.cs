using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CCWaterControllerPlayer.Models;
using CCWaterControllerPlayer.Services;
using CCWaterControllerPlayer.Views;

namespace CCWaterControllerPlayer.ViewModels;

public enum FullscreenMode
{
    None,
    LeftStick,
    RightStick,
    BothSticks,
    Playback
}

public partial class RecordsViewModel : ViewModelBase
{
    private readonly DatabaseService _databaseService;
    private readonly DispatcherTimer _playbackTimer;
    private readonly Stopwatch _playbackStopwatch = new();
    private double _playbackElapsedAtPause;
    private double _dataDurationMs;
    private int _displayStep = 1;
    private List<ControllerSnapshot> _fullSnapshots = new();
    private List<double> _snapshotTimesMs = new();
    private const double PlaybackWindowMs = 3000;
    private const int MaxDisplayPoints = 600;

    private double _compareElapsedAtPause;
    private double _compareDurationMs;
    private int _compareDisplayStep = 1;
    private List<ControllerSnapshot> _compareSnapshots = new();
    private List<double> _compareTimesMs = new();

    [ObservableProperty]
    private ObservableCollection<TrackRecord> _records = new();

    [ObservableProperty]
    private ObservableCollection<TrackRecord> _selectedRecords = new();

    [ObservableProperty]
    private TrackRecord? _selectedRecord;

    [ObservableProperty]
    private TrackRecord? _compareRecord;

    [ObservableProperty]
    private bool _isCompareMode;

    [ObservableProperty]
    private double _playbackSpeed = 1.0;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double _playbackProgress;

    [ObservableProperty]
    private double _compareProgress;

    private bool _isSeekingFromPlayback;

    [ObservableProperty]
    private ObservableCollection<TrackPoint> _leftStickTrackPoints = new();

    [ObservableProperty]
    private ObservableCollection<TrackPoint> _rightStickTrackPoints = new();

    [ObservableProperty]
    private ObservableCollection<List<TrackPoint>> _leftStickHistoryTracks = new();

    [ObservableProperty]
    private ObservableCollection<List<TrackPoint>> _rightStickHistoryTracks = new();

    [ObservableProperty]
    private ObservableCollection<TimeSeriesPoint> _leftTimeSeriesX = new();

    [ObservableProperty]
    private ObservableCollection<TimeSeriesPoint> _leftTimeSeriesY = new();

    [ObservableProperty]
    private ObservableCollection<TimeSeriesPoint> _rightTimeSeriesX = new();

    [ObservableProperty]
    private ObservableCollection<TimeSeriesPoint> _rightTimeSeriesY = new();

    [ObservableProperty]
    private float _currentLeftX;

    [ObservableProperty]
    private float _currentLeftY;

    [ObservableProperty]
    private float _currentRightX;

    [ObservableProperty]
    private float _currentRightY;

    [ObservableProperty]
    private FullscreenMode _currentFullscreenMode = FullscreenMode.None;

    [ObservableProperty]
    private bool _isFullscreen;

    [ObservableProperty]
    private ObservableCollection<RecoilPoint> _analysisInputTrack = new();

    [ObservableProperty]
    private ObservableCollection<RecoilPoint> _analysisRecoilTrack = new();

    [ObservableProperty]
    private int _analysisCurrentIndex = -1;

    [ObservableProperty]
    private double _triggerStartMs;

    [ObservableProperty]
    private double _triggerEndMs;

    [ObservableProperty]
    private bool _showInputTrack = true;

    [ObservableProperty]
    private bool _showRecoilTrack = true;

    private List<RecoilPoint> _fullInputTrack = new();
    private List<RecoilPoint> _fullRecoilTrack = new();
    private List<double> _analysisTimesMs = new();

    public Action<TrackRecord>? OnSendToOverlay;
    public Action? OnClearOverlay;

    public RecordsViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _playbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _playbackTimer.Tick += OnPlaybackTick;
    }

    public async Task LoadRecordsAsync()
    {
        var records = await _databaseService.GetTrackRecordsAsync(null, null);
        Records.Clear();
        foreach (var record in records)
            Records.Add(record);
    }

    public void AddRecord(TrackRecord record)
    {
        Records.Insert(0, record);
    }

    partial void OnSelectedRecordChanged(TrackRecord? value)
    {
        if (value == null) return;
        StopPlaybackInternal();
        _ = LoadAndDisplayRecordAsync(value);
    }

    private async Task LoadAndDisplayRecordAsync(TrackRecord record)
    {
        await _databaseService.LoadSnapshotsAsync(record);
        DisplayFullRecord(record);
    }

    private void DisplayFullRecord(TrackRecord record)
    {
        _fullSnapshots = record.Snapshots;
        _snapshotTimesMs.Clear();

        if (_fullSnapshots.Count == 0) return;

        long baseTime = _fullSnapshots[0].TimestampTicks;
        foreach (var snapshot in _fullSnapshots)
        {
            double ms = (snapshot.TimestampTicks - baseTime) / (double)TimeSpan.TicksPerMillisecond;
            _snapshotTimesMs.Add(ms);
        }

        _dataDurationMs = _snapshotTimesMs[^1];
        _playbackElapsedAtPause = _dataDurationMs;

        int samplesPerWindow = (int)(_fullSnapshots.Count * (PlaybackWindowMs / Math.Max(1, _dataDurationMs)));
        _displayStep = Math.Max(1, samplesPerWindow / MaxDisplayPoints);

        TriggerStartMs = (record.TriggerStartTicks - baseTime) / (double)TimeSpan.TicksPerMillisecond;
        TriggerEndMs = (record.TriggerEndTicks - baseTime) / (double)TimeSpan.TicksPerMillisecond;

        ComputeAnalysisTracks();
        DisplayDataAtTime(_dataDurationMs);
        UpdateAnalysisAtTime(_dataDurationMs);

        _isSeekingFromPlayback = true;
        PlaybackProgress = 100;
        _isSeekingFromPlayback = false;
    }

    private void DisplayDataAtTime(double currentTimeMs)
    {
        double windowStart = currentTimeMs - PlaybackWindowMs;

        int startIdx = FindFirstIndexAfter(windowStart);
        int endIdx = FindIndexAtTime(currentTimeMs);

        if (startIdx < 0) startIdx = 0;
        if (endIdx < 0) endIdx = 0;

        var leftTrack = new ObservableCollection<TrackPoint>();
        var rightTrack = new ObservableCollection<TrackPoint>();
        var ltx = new ObservableCollection<TimeSeriesPoint>();
        var lty = new ObservableCollection<TimeSeriesPoint>();
        var rtx = new ObservableCollection<TimeSeriesPoint>();
        var rty = new ObservableCollection<TimeSeriesPoint>();

        int alignedStart = startIdx - (startIdx % _displayStep);
        if (alignedStart < startIdx) alignedStart += _displayStep;

        for (int i = alignedStart; i <= endIdx && i < _fullSnapshots.Count; i += _displayStep)
        {
            double ms = _snapshotTimesMs[i];
            var s = _fullSnapshots[i];
            leftTrack.Add(new TrackPoint(s.LeftStick.X, s.LeftStick.Y, ms));
            rightTrack.Add(new TrackPoint(s.RightStick.X, s.RightStick.Y, ms));
            ltx.Add(new TimeSeriesPoint(ms, s.LeftStick.X));
            lty.Add(new TimeSeriesPoint(ms, s.LeftStick.Y));
            rtx.Add(new TimeSeriesPoint(ms, s.RightStick.X));
            rty.Add(new TimeSeriesPoint(ms, s.RightStick.Y));
        }

        if (endIdx >= 0 && endIdx < _fullSnapshots.Count)
        {
            var s = _fullSnapshots[endIdx];
            double ms = _snapshotTimesMs[endIdx];
            if (leftTrack.Count == 0 || leftTrack[^1].TimestampMs != ms)
            {
                leftTrack.Add(new TrackPoint(s.LeftStick.X, s.LeftStick.Y, ms));
                rightTrack.Add(new TrackPoint(s.RightStick.X, s.RightStick.Y, ms));
                ltx.Add(new TimeSeriesPoint(ms, s.LeftStick.X));
                lty.Add(new TimeSeriesPoint(ms, s.LeftStick.Y));
                rtx.Add(new TimeSeriesPoint(ms, s.RightStick.X));
                rty.Add(new TimeSeriesPoint(ms, s.RightStick.Y));
            }
            CurrentLeftX = s.LeftStick.X;
            CurrentLeftY = s.LeftStick.Y;
            CurrentRightX = s.RightStick.X;
            CurrentRightY = s.RightStick.Y;
        }

        LeftStickTrackPoints = leftTrack;
        RightStickTrackPoints = rightTrack;
        LeftTimeSeriesX = ltx;
        LeftTimeSeriesY = lty;
        RightTimeSeriesX = rtx;
        RightTimeSeriesY = rty;
    }

    private void ComputeAnalysisTracks()
    {
        _fullInputTrack.Clear();
        _fullRecoilTrack.Clear();
        _analysisTimesMs.Clear();

        if (_fullSnapshots.Count == 0) return;

        long baseTime = _fullSnapshots[0].TimestampTicks;
        float posX = 0, posY = 0;
        double prevMs = 0;
        IStickCurve stickCurve = new LinearCurve();

        for (int i = 0; i < _fullSnapshots.Count; i++)
        {
            var s = _fullSnapshots[i];
            double ms = (s.TimestampTicks - baseTime) / (double)TimeSpan.TicksPerMillisecond;
            double dt = (i == 0) ? 0 : (ms - prevMs) / 1000.0;

            float rawX = s.RightStick.X;
            float rawY = s.RightStick.Y;
            float mappedX = stickCurve.Apply(rawX);
            float mappedY = stickCurve.Apply(rawY);

            posX += mappedX * (float)dt;
            posY += mappedY * (float)dt;

            _fullInputTrack.Add(new RecoilPoint(posX, posY, ms));
            _fullRecoilTrack.Add(new RecoilPoint(-posX, -posY, ms));
            _analysisTimesMs.Add(ms);
            prevMs = ms;
        }

        int step = Math.Max(1, _fullInputTrack.Count / 1000);

        var inputDisplay = new ObservableCollection<RecoilPoint>();
        var recoilDisplay = new ObservableCollection<RecoilPoint>();

        for (int i = 0; i < _fullInputTrack.Count; i += step)
        {
            inputDisplay.Add(_fullInputTrack[i]);
            recoilDisplay.Add(_fullRecoilTrack[i]);
        }

        if (_fullInputTrack.Count > 0 && (_fullInputTrack.Count - 1) % step != 0)
        {
            inputDisplay.Add(_fullInputTrack[^1]);
            recoilDisplay.Add(_fullRecoilTrack[^1]);
        }

        AnalysisInputTrack = inputDisplay;
        AnalysisRecoilTrack = recoilDisplay;
        AnalysisCurrentIndex = inputDisplay.Count - 1;
    }

    private void UpdateAnalysisAtTime(double currentTimeMs)
    {
        if (_fullInputTrack.Count == 0) return;
        int idx = FindIndexInAnalysis(currentTimeMs);
        int step = Math.Max(1, _fullInputTrack.Count / 1000);
        int displayIdx = idx / step;
        if (displayIdx >= AnalysisInputTrack.Count)
            displayIdx = AnalysisInputTrack.Count - 1;
        if (displayIdx < 0) displayIdx = 0;
        AnalysisCurrentIndex = displayIdx;
    }

    private int FindFirstIndexAfter(double timeMs)
    {
        int lo = 0, hi = _snapshotTimesMs.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (_snapshotTimesMs[mid] < timeMs) lo = mid + 1;
            else hi = mid - 1;
        }
        return lo;
    }

    private int FindIndexAtTime(double timeMs)
    {
        int lo = 0, hi = _snapshotTimesMs.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (_snapshotTimesMs[mid] <= timeMs) lo = mid + 1;
            else hi = mid - 1;
        }
        return Math.Min(hi, _snapshotTimesMs.Count - 1);
    }

    private async void LoadCompareRecord(TrackRecord record)
    {
        await _databaseService.LoadSnapshotsAsync(record);
        _compareSnapshots = record.Snapshots;
        _compareTimesMs.Clear();

        if (_compareSnapshots.Count == 0) return;

        long baseTime = _compareSnapshots[0].TimestampTicks;
        foreach (var snapshot in _compareSnapshots)
        {
            double ms = (snapshot.TimestampTicks - baseTime) / (double)TimeSpan.TicksPerMillisecond;
            _compareTimesMs.Add(ms);
        }

        _compareDurationMs = _compareTimesMs[^1];
        _compareElapsedAtPause = _compareDurationMs;

        int samplesPerWindow = (int)(_compareSnapshots.Count * (PlaybackWindowMs / Math.Max(1, _compareDurationMs)));
        _compareDisplayStep = Math.Max(1, samplesPerWindow / MaxDisplayPoints);

        DisplayCompareAtTime(_compareDurationMs);

        _isSeekingFromPlayback = true;
        CompareProgress = 100;
        _isSeekingFromPlayback = false;
    }

    private void DisplayCompareAtTime(double currentTimeMs)
    {
        if (_compareSnapshots.Count == 0 || _compareTimesMs.Count == 0) return;

        double windowStart = currentTimeMs - PlaybackWindowMs;
        int startIdx = FindFirstIndexInList(_compareTimesMs, windowStart);
        int endIdx = FindLastIndexInList(_compareTimesMs, currentTimeMs);

        if (startIdx < 0) startIdx = 0;
        if (endIdx < 0) endIdx = 0;

        var leftTrack = new List<TrackPoint>();
        var rightTrack = new List<TrackPoint>();

        int alignedStart = startIdx - (startIdx % _compareDisplayStep);
        if (alignedStart < startIdx) alignedStart += _compareDisplayStep;

        for (int i = alignedStart; i <= endIdx && i < _compareSnapshots.Count; i += _compareDisplayStep)
        {
            double ms = _compareTimesMs[i];
            var s = _compareSnapshots[i];
            leftTrack.Add(new TrackPoint(s.LeftStick.X, s.LeftStick.Y, ms));
            rightTrack.Add(new TrackPoint(s.RightStick.X, s.RightStick.Y, ms));
        }

        if (endIdx >= 0 && endIdx < _compareSnapshots.Count)
        {
            var s = _compareSnapshots[endIdx];
            double ms = _compareTimesMs[endIdx];
            if (leftTrack.Count == 0 || leftTrack[^1].TimestampMs != ms)
            {
                leftTrack.Add(new TrackPoint(s.LeftStick.X, s.LeftStick.Y, ms));
                rightTrack.Add(new TrackPoint(s.RightStick.X, s.RightStick.Y, ms));
            }
        }

        LeftStickHistoryTracks = new ObservableCollection<List<TrackPoint>> { leftTrack };
        RightStickHistoryTracks = new ObservableCollection<List<TrackPoint>> { rightTrack };
    }

    private static int FindFirstIndexInList(List<double> times, double timeMs)
    {
        int lo = 0, hi = times.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (times[mid] < timeMs) lo = mid + 1;
            else hi = mid - 1;
        }
        return lo;
    }

    private static int FindLastIndexInList(List<double> times, double timeMs)
    {
        int lo = 0, hi = times.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (times[mid] <= timeMs) lo = mid + 1;
            else hi = mid - 1;
        }
        return Math.Min(hi, times.Count - 1);
    }

    [RelayCommand]
    private void ToggleRecordForCompare(TrackRecord? record)
    {
        if (record == null) return;

        if (CompareRecord == record)
        {
            CompareRecord = null;
            _compareSnapshots.Clear();
            _compareTimesMs.Clear();
            LeftStickHistoryTracks = new();
            RightStickHistoryTracks = new();
            CompareProgress = 0;
            SelectedRecords.Remove(record);
        }
        else
        {
            if (CompareRecord != null)
                SelectedRecords.Remove(CompareRecord);
            CompareRecord = record;
            if (!SelectedRecords.Contains(record))
                SelectedRecords.Add(record);
            LoadCompareRecord(record);
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        StopPlaybackInternal();
        CompareRecord = null;
        _compareSnapshots.Clear();
        _compareTimesMs.Clear();
        SelectedRecords.Clear();
        LeftStickHistoryTracks = new();
        RightStickHistoryTracks = new();
        LeftStickTrackPoints = new();
        RightStickTrackPoints = new();
        LeftTimeSeriesX = new();
        LeftTimeSeriesY = new();
        RightTimeSeriesX = new();
        RightTimeSeriesY = new();
        AnalysisInputTrack = new();
        AnalysisRecoilTrack = new();
        AnalysisCurrentIndex = -1;
        _isSeekingFromPlayback = true;
        PlaybackProgress = 0;
        CompareProgress = 0;
        _isSeekingFromPlayback = false;
        SelectedRecord = null;
    }

    [RelayCommand]
    private void ToggleCompareMode()
    {
        IsCompareMode = !IsCompareMode;
        if (!IsCompareMode)
        {
            SelectedRecords.Clear();
            LeftStickHistoryTracks.Clear();
            RightStickHistoryTracks.Clear();
        }
    }

    [RelayCommand]
    private async Task DeleteRecordAsync(TrackRecord? record)
    {
        if (record == null) return;
        await _databaseService.DeleteTrackRecordAsync(record.Id);
        Records.Remove(record);
        SelectedRecords.Remove(record);
        if (SelectedRecord == record)
        {
            SelectedRecord = null;
            LeftStickTrackPoints = new();
            RightStickTrackPoints = new();
            LeftTimeSeriesX = new();
            LeftTimeSeriesY = new();
            RightTimeSeriesX = new();
            RightTimeSeriesY = new();
            AnalysisInputTrack = new();
            AnalysisRecoilTrack = new();
            AnalysisCurrentIndex = -1;
        }
    }

    [ObservableProperty]
    private string _renameText = string.Empty;

    [ObservableProperty]
    private TrackRecord? _renamingRecord;

    [RelayCommand]
    private void StartRename(TrackRecord? record)
    {
        if (record == null) return;
        RenamingRecord = record;
        RenameText = record.Name;
    }

    [RelayCommand]
    private async Task ConfirmRenameAsync()
    {
        if (RenamingRecord == null) return;
        RenamingRecord.Name = RenameText.Trim();
        await _databaseService.UpdateRecordNameAsync(RenamingRecord.Id, RenamingRecord.Name);
        var idx = Records.IndexOf(RenamingRecord);
        if (idx >= 0)
        {
            Records.RemoveAt(idx);
            Records.Insert(idx, RenamingRecord);
        }
        RenamingRecord = null;
        RenameText = string.Empty;
    }

    [RelayCommand]
    private void CancelRename()
    {
        RenamingRecord = null;
        RenameText = string.Empty;
    }

    [RelayCommand]
    private async Task SendToOverlayAsync(TrackRecord? record)
    {
        if (record == null) return;
        await _databaseService.LoadSnapshotsAsync(record);
        OnSendToOverlay?.Invoke(record);
    }

    [RelayCommand]
    private void ClearOverlayHistory()
    {
        OnClearOverlay?.Invoke();
    }

    [RelayCommand]
    private async Task ToggleRecordStatusAsync(TrackRecord? record)
    {
        if (record == null) return;
        record.Status = record.Status == RecordStatus.Temporary
            ? RecordStatus.Permanent
            : RecordStatus.Temporary;
        await _databaseService.UpdateRecordStatusAsync(record.Id, record.Status);
        var idx = Records.IndexOf(record);
        if (idx >= 0)
        {
            Records.RemoveAt(idx);
            Records.Insert(idx, record);
        }
    }

    [RelayCommand]
    private async Task DeleteByStatusAsync(RecordStatus status)
    {
        await _databaseService.DeleteRecordsByStatusAsync(status);
        var toRemove = Records.Where(r => r.Status == status).ToList();
        foreach (var r in toRemove)
        {
            Records.Remove(r);
            SelectedRecords.Remove(r);
            if (SelectedRecord == r) SelectedRecord = null;
        }
    }

    [RelayCommand]
    private void PlayPause()
    {
        bool hasMain = _fullSnapshots.Count > 0 && _dataDurationMs > 0;
        bool hasCompare = _compareSnapshots.Count > 0 && _compareDurationMs > 0;
        if (!hasMain && !hasCompare) return;

        if (IsPlaying)
        {
            double elapsed = _playbackStopwatch.Elapsed.TotalMilliseconds * PlaybackSpeed;
            _playbackElapsedAtPause += elapsed;
            _compareElapsedAtPause += elapsed;
            _playbackStopwatch.Stop();
            _playbackTimer.Stop();
            IsPlaying = false;
        }
        else
        {
            if (hasMain && _playbackElapsedAtPause >= _dataDurationMs &&
                (!hasCompare || _compareElapsedAtPause >= _compareDurationMs))
            {
                _playbackElapsedAtPause = 0;
                _compareElapsedAtPause = 0;
            }

            _playbackStopwatch.Restart();
            IsPlaying = true;
            _playbackTimer.Start();
        }
    }

    [RelayCommand]
    private void StopPlayback()
    {
        StopPlaybackInternal();
        _playbackElapsedAtPause = 0;
        _compareElapsedAtPause = 0;

        _isSeekingFromPlayback = true;
        PlaybackProgress = 0;
        CompareProgress = 0;
        _isSeekingFromPlayback = false;

        if (_fullSnapshots.Count > 0)
        {
            DisplayDataAtTime(0);
            UpdateAnalysisAtTime(0);
        }
        if (_compareSnapshots.Count > 0)
            DisplayCompareAtTime(0);
    }

    private void StopPlaybackInternal()
    {
        _playbackTimer.Stop();
        _playbackStopwatch.Stop();
        IsPlaying = false;
    }

    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        double elapsed = _playbackStopwatch.Elapsed.TotalMilliseconds * PlaybackSpeed;
        bool mainDone = true;
        bool compareDone = true;

        _isSeekingFromPlayback = true;

        if (_fullSnapshots.Count > 0 && _dataDurationMs > 0)
        {
            double mainTimeMs = _playbackElapsedAtPause + elapsed;
            if (mainTimeMs >= _dataDurationMs)
            {
                mainTimeMs = _dataDurationMs;
                PlaybackProgress = 100;
            }
            else
            {
                mainDone = false;
                PlaybackProgress = mainTimeMs / _dataDurationMs * 100;
            }
            DisplayDataAtTime(mainTimeMs);
            UpdateAnalysisAtTime(mainTimeMs);
        }

        if (_compareSnapshots.Count > 0 && _compareDurationMs > 0)
        {
            double compareTimeMs = _compareElapsedAtPause + elapsed;
            if (compareTimeMs >= _compareDurationMs)
            {
                compareTimeMs = _compareDurationMs;
                CompareProgress = 100;
            }
            else
            {
                compareDone = false;
                CompareProgress = compareTimeMs / _compareDurationMs * 100;
            }
            DisplayCompareAtTime(compareTimeMs);
        }

        _isSeekingFromPlayback = false;

        if (mainDone && compareDone)
        {
            double finalElapsed = _playbackStopwatch.Elapsed.TotalMilliseconds * PlaybackSpeed;
            _playbackElapsedAtPause += finalElapsed;
            _compareElapsedAtPause += finalElapsed;
            _playbackStopwatch.Stop();
            _playbackTimer.Stop();
            IsPlaying = false;
        }
    }

    partial void OnPlaybackProgressChanged(double value)
    {
        if (_isSeekingFromPlayback) return;
        if (_fullSnapshots.Count > 0 && _dataDurationMs > 0)
        {
            _playbackElapsedAtPause = value / 100.0 * _dataDurationMs;
            if (IsPlaying)
            {
                _playbackStopwatch.Restart();
            }
            else
            {
                DisplayDataAtTime(_playbackElapsedAtPause);
                UpdateAnalysisAtTime(_playbackElapsedAtPause);
            }
        }
    }

    partial void OnCompareProgressChanged(double value)
    {
        if (_isSeekingFromPlayback) return;
        if (_compareSnapshots.Count > 0 && _compareDurationMs > 0)
        {
            _compareElapsedAtPause = value / 100.0 * _compareDurationMs;
            if (IsPlaying)
            {
                _playbackStopwatch.Restart();
            }
            else
            {
                DisplayCompareAtTime(_compareElapsedAtPause);
            }
        }
    }

    partial void OnPlaybackSpeedChanged(double value)
    {
        if (value < 0.25) PlaybackSpeed = 0.25;
        if (value > 4.0) PlaybackSpeed = 4.0;
    }

    [RelayCommand]
    private void EnterFullscreen(FullscreenMode mode)
    {
        CurrentFullscreenMode = mode;
        IsFullscreen = true;
    }

    [RelayCommand]
    private void ExitFullscreen()
    {
        CurrentFullscreenMode = FullscreenMode.None;
        IsFullscreen = false;
    }

    [RelayCommand]
    private void ToggleInputTrack()
    {
        ShowInputTrack = !ShowInputTrack;
    }

    [RelayCommand]
    private void ToggleRecoilTrack()
    {
        ShowRecoilTrack = !ShowRecoilTrack;
    }

    private int FindIndexInAnalysis(double timeMs)
    {
        int lo = 0, hi = _analysisTimesMs.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (_analysisTimesMs[mid] <= timeMs) lo = mid + 1;
            else hi = mid - 1;
        }
        return Math.Clamp(hi, 0, _analysisTimesMs.Count - 1);
    }
}
