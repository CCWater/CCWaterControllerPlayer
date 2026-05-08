using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CCWaterControllerPlayer.Models;
using CCWaterControllerPlayer.Overlay;
using CCWaterControllerPlayer.Services;
using CCWaterControllerPlayer.Views;

namespace CCWaterControllerPlayer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly DatabaseService _databaseService;
    private readonly RecordingService _recordingService;
    private IControllerService? _controllerService;
    private OverlayWindow? _overlayWindow;
    private ImageOverlayWindow? _imageOverlayWindow;
    private bool _wasTriggerActive;
    private long _lastUiUpdateTicks;
    private static readonly long UiUpdateIntervalTicks = Stopwatch.Frequency / 60;
    private CancellationTokenSource? _autoDetectCts;
    private bool _isAutoDetecting;
    private CancellationTokenSource? _saveDebounceCts;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private bool _isControllerConnected;

    [ObservableProperty]
    private string _controllerName = "No Controller";

    [ObservableProperty]
    private int _samplingRate;

    [ObservableProperty]
    private RecordingState _recordingState;

    [ObservableProperty]
    private int _recordCount;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private StickPosition _currentLeftStick;

    [ObservableProperty]
    private StickPosition _currentRightStick;

    [ObservableProperty]
    private TriggerState _currentTriggers;

    [ObservableProperty]
    private bool _isOverlayVisible;

    [ObservableProperty]
    private bool _isImageOverlayVisible;

    public MonitorViewModel MonitorView { get; }
    public RecordsViewModel RecordsView { get; }
    public SettingsViewModel SettingsView { get; }
    public HelpViewModel HelpView { get; }
    public OverlayViewModel OverlayView { get; }

    public MainViewModel(
        SettingsService settingsService,
        DatabaseService databaseService,
        RecordingService recordingService)
    {
        _settingsService = settingsService;
        _databaseService = databaseService;
        _recordingService = recordingService;

        MonitorView = new MonitorViewModel(this);
        RecordsView = new RecordsViewModel(databaseService);
        SettingsView = new SettingsViewModel(settingsService);
        HelpView = new HelpViewModel();
        OverlayView = new OverlayViewModel(settingsService);

        OverlayView.OnToggleOverlay = () => ToggleOverlay();
        OverlayView.OnPinOverlay = () => PinOverlay();
        OverlayView.OnUnpinOverlay = () => UnpinOverlay();
        OverlayView.OnSettingsApplied = () => ApplyOverlaySettings();
        OverlayView.OnToggleImageOverlay = () => ToggleImageOverlay();
        OverlayView.OnPinImageOverlay = () => PinImageOverlay();
        OverlayView.OnUnpinImageOverlay = () => UnpinImageOverlay();
        OverlayView.OnSelectImage = (path) => SetImageOverlayImage(path);
        OverlayView.GetIsOverlayVisible = () => IsOverlayVisible;
        OverlayView.GetIsOverlayPinned = () => _overlayWindow?.IsPinned ?? false;
        OverlayView.GetIsImageOverlayVisible = () => IsImageOverlayVisible;
        OverlayView.GetIsImageOverlayPinned = () => _imageOverlayWindow?.IsPinned ?? false;

        SettingsView.OnSettingsSaved = ApplySettings;
        RecordsView.OnSendToOverlay = SetOverlayHistoryRecord;
        RecordsView.OnClearOverlay = ClearOverlayHistory;

        _recordingService.RecordingCompleted += OnRecordingCompleted;
        _recordingService.StateChanged += OnRecordingStateChanged;

        CurrentView = MonitorView;
    }

    public async Task InitializeAsync()
    {
        await _settingsService.LoadAsync();
        await _databaseService.InitializeAsync();
        await RecordsView.LoadRecordsAsync();

        if (!string.IsNullOrEmpty(_settingsService.Settings.Language))
        {
            LocalizationService.Instance.SetLanguage(_settingsService.Settings.Language);
        }

        if (_settingsService.Settings.OverlayConfig.Enabled)
        {
            ShowOverlay();
            await LoadLatestRecordToOverlay();
        }

        StartAutoDetect();

        if (!_settingsService.Settings.HasConfiguredTrigger)
        {
            ShowFirstRunGuide();
        }
    }

    private void ShowFirstRunGuide()
    {
        var lang = LocalizationService.Instance.Strings;
        var result = System.Windows.MessageBox.Show(
            lang.FirstRunMessage,
            lang.FirstRunTitle,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Information);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            CurrentView = SettingsView;
        }

        _settingsService.Settings.HasConfiguredTrigger = true;
        _ = _settingsService.SaveAsync();
    }

    public void RestoreMainWindowState(System.Windows.Window window)
    {
        var state = _settingsService.Settings.MainWindowState;
        if (!double.IsNaN(state.Left) && !double.IsNaN(state.Top))
        {
            window.Left = state.Left;
            window.Top = state.Top;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
        }
        window.Width = state.Width;
        window.Height = state.Height;
        if (state.IsMaximized)
            window.WindowState = System.Windows.WindowState.Maximized;
    }

    public void SaveMainWindowState(System.Windows.Window window)
    {
        var state = _settingsService.Settings.MainWindowState;
        if (window.WindowState == System.Windows.WindowState.Normal)
        {
            state.Left = window.Left;
            state.Top = window.Top;
            state.Width = window.Width;
            state.Height = window.Height;
        }
        state.IsMaximized = window.WindowState == System.Windows.WindowState.Maximized;
    }

    private void ToggleOverlay()
    {
        if (IsOverlayVisible)
            HideOverlay();
        else
            ShowOverlay();
    }

    private void ShowOverlay()
    {
        if (_overlayWindow == null)
        {
            _overlayWindow = new OverlayWindow();
            _overlayWindow.OnPositionChanged = () => SyncOverlayPositionToSettings();
        }

        var config = _settingsService.Settings.OverlayConfig;
        _overlayWindow.Configure(config);
        _overlayWindow.Show();

        IsOverlayVisible = true;
        StatusMessage = "Overlay shown";
    }

    private void SyncOverlayPositionToSettings()
    {
        if (_overlayWindow == null) return;
        var config = _settingsService.Settings.OverlayConfig;
        config.PositionX = (int)_overlayWindow.Left;
        config.PositionY = (int)_overlayWindow.Top;
        config.Width = (int)_overlayWindow.Width;
        config.Height = (int)_overlayWindow.Height;
        ScheduleSave();
    }

    private void HideOverlay()
    {
        _overlayWindow?.Hide();
        IsOverlayVisible = false;
        StatusMessage = "Overlay hidden";
    }

    private async Task LoadLatestRecordToOverlay()
    {
        if (_overlayWindow == null || !IsOverlayVisible) return;

        var latestRecord = await _databaseService.GetLatestRecordAsync();
        if (latestRecord != null)
        {
            ShowRecoilOnOverlay(latestRecord);
        }
    }

    private void ShowRecoilOnOverlay(TrackRecord record)
    {
        if (_overlayWindow == null || !IsOverlayVisible) return;
        if (record.Snapshots.Count == 0) return;

        long baseTime = record.Snapshots[0].TimestampTicks;
        double triggerStartMs = (record.TriggerStartTicks - baseTime) / (double)TimeSpan.TicksPerMillisecond;
        double triggerEndMs = (record.TriggerEndTicks - baseTime) / (double)TimeSpan.TicksPerMillisecond;

        IStickCurve stickCurve = new LinearCurve();
        float posX = 0, posY = 0;
        double prevMs = 0;

        var inputTrack = new ObservableCollection<RecoilPoint>();
        var recoilTrack = new ObservableCollection<RecoilPoint>();

        int step = Math.Max(1, record.Snapshots.Count / 1000);

        for (int i = 0; i < record.Snapshots.Count; i++)
        {
            var s = record.Snapshots[i];
            double ms = (s.TimestampTicks - baseTime) / (double)TimeSpan.TicksPerMillisecond;
            double dt = (i == 0) ? 0 : (ms - prevMs) / 1000.0;

            float mappedX = stickCurve.Apply(s.RightStick.X);
            float mappedY = stickCurve.Apply(s.RightStick.Y);
            posX += mappedX * (float)dt;
            posY += mappedY * (float)dt;

            if (i % step == 0 || i == record.Snapshots.Count - 1)
            {
                inputTrack.Add(new RecoilPoint(posX, posY, ms));
                recoilTrack.Add(new RecoilPoint(-posX, -posY, ms));
            }

            prevMs = ms;
        }

        _overlayWindow.SetRecoilData(inputTrack, recoilTrack, triggerStartMs, triggerEndMs);
    }

    public void SetOverlayHistoryRecord(TrackRecord record)
    {
        if (_overlayWindow == null || !IsOverlayVisible) return;

        if (_settingsService.Settings.OverlayConfig.ShowHistory)
            _overlayWindow.SetHistoryRecord(record);

        if (_settingsService.Settings.OverlayConfig.ShowRecoilAnalysis)
            ShowRecoilOnOverlay(record);
    }

    public void ClearOverlayHistory()
    {
        _overlayWindow?.ClearHistory();
        _overlayWindow?.ClearRecoilData();
    }

    private void PinOverlay()
    {
        if (_overlayWindow != null && IsOverlayVisible && !_overlayWindow.IsPinned)
        {
            _overlayWindow.Pin();
            StatusMessage = "Overlay pinned";
        }
    }

    private void UnpinOverlay()
    {
        if (_overlayWindow != null && _overlayWindow.IsPinned)
        {
            _overlayWindow.Unpin();
            StatusMessage = "Overlay unpinned";
        }
    }

    private void ApplyOverlaySettings()
    {
        if (_overlayWindow != null && IsOverlayVisible)
        {
            var config = _settingsService.Settings.OverlayConfig;
            _overlayWindow.Configure(config);
        }

        if (_imageOverlayWindow != null && IsImageOverlayVisible)
        {
            var imgConfig = _settingsService.Settings.ImageOverlayConfig;
            _imageOverlayWindow.Opacity = imgConfig.Opacity;
        }
    }

    private void ToggleImageOverlay()
    {
        if (IsImageOverlayVisible)
            HideImageOverlay();
        else
            ShowImageOverlay();
    }

    private void ShowImageOverlay()
    {
        if (_imageOverlayWindow == null)
        {
            _imageOverlayWindow = new ImageOverlayWindow();
            _imageOverlayWindow.OnImageChanged = (path) =>
            {
                _settingsService.Settings.ImageOverlayConfig.ImagePath = path;
                ScheduleSave();
            };
            _imageOverlayWindow.OnPositionChanged = () => SyncImageOverlayPositionToSettings();
        }

        var config = _settingsService.Settings.ImageOverlayConfig;
        _imageOverlayWindow.Configure(config);
        _imageOverlayWindow.Show();
        IsImageOverlayVisible = true;
    }

    private void SyncImageOverlayPositionToSettings()
    {
        if (_imageOverlayWindow == null) return;
        var config = _settingsService.Settings.ImageOverlayConfig;
        config.PositionX = (int)_imageOverlayWindow.Left;
        config.PositionY = (int)_imageOverlayWindow.Top;
        config.Width = (int)_imageOverlayWindow.Width;
        config.Height = (int)_imageOverlayWindow.Height;
        ScheduleSave();
    }

    private void HideImageOverlay()
    {
        _imageOverlayWindow?.Hide();
        IsImageOverlayVisible = false;
    }

    private void PinImageOverlay()
    {
        if (_imageOverlayWindow != null && IsImageOverlayVisible && !_imageOverlayWindow.IsPinned)
        {
            _imageOverlayWindow.Pin();
        }
    }

    private void UnpinImageOverlay()
    {
        if (_imageOverlayWindow != null && _imageOverlayWindow.IsPinned)
        {
            _imageOverlayWindow.Unpin();
        }
    }

    private void SetImageOverlayImage(string path)
    {
        if (_imageOverlayWindow == null) ShowImageOverlay();
        _imageOverlayWindow!.LoadImage(path);
        _settingsService.Settings.ImageOverlayConfig.ImagePath = path;
        ScheduleSave();
    }

    private void ScheduleSave()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts = new CancellationTokenSource();
        var token = _saveDebounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                    await _settingsService.SaveAsync();
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private void ApplySettings()
    {
        var settings = _settingsService.Settings;

        if (_controllerService != null)
        {
            _controllerService.TargetPollingRateHz = Math.Clamp(settings.SamplingRateHz, 100, 2000);
        }

        _recordingService.UpdateSettings(settings);

        if (_overlayWindow != null && IsOverlayVisible && !_overlayWindow.IsPinned)
        {
            _overlayWindow.Configure(settings.OverlayConfig);
        }

        StatusMessage = "Settings applied";
    }

    [RelayCommand]
    private void NavigateToMonitor() => CurrentView = MonitorView;

    [RelayCommand]
    private void NavigateToRecords() => CurrentView = RecordsView;

    [RelayCommand]
    private void NavigateToSettings() => CurrentView = SettingsView;

    [RelayCommand]
    private void NavigateToHelp() => CurrentView = HelpView;

    [RelayCommand]
    private void NavigateToOverlay()
    {
        OverlayView.RefreshState();
        CurrentView = OverlayView;
    }

    private void StartAutoDetect()
    {
        if (_isAutoDetecting) return;
        _isAutoDetecting = true;
        _autoDetectCts = new CancellationTokenSource();
        _ = AutoDetectLoopAsync(_autoDetectCts.Token);
    }

    private void StopAutoDetect()
    {
        _isAutoDetecting = false;
        _autoDetectCts?.Cancel();
        _autoDetectCts?.Dispose();
        _autoDetectCts = null;
    }

    private async Task AutoDetectLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_controllerService == null)
            {
                await TryConnectControllerAsync();
            }

            try
            {
                await Task.Delay(2000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    [RelayCommand]
    private async Task ConnectControllerAsync()
    {
        if (_controllerService != null)
        {
            await DisconnectControllerAsync();
            return;
        }

        await TryConnectControllerAsync();
    }

    private async Task TryConnectControllerAsync()
    {
        try
        {
            int targetRate = Math.Clamp(_settingsService.Settings.SamplingRateHz, 100, 2000);

            var xinput = new XInputControllerService();
            var devices = xinput.EnumerateDevices();
            if (devices.Count > 0)
            {
                xinput.TargetPollingRateHz = targetRate;
                _controllerService = xinput;
            }
            else
            {
                xinput.Dispose();
                var dinput = new DirectInputControllerService();
                devices = dinput.EnumerateDevices();
                if (devices.Count > 0)
                {
                    dinput.TargetPollingRateHz = targetRate;
                    _controllerService = dinput;
                }
                else
                {
                    dinput.Dispose();
                    return;
                }
            }

            _controllerService.InputReceived += OnInputReceived;
            _controllerService.DeviceConnected += OnDeviceConnected;
            _controllerService.DeviceDisconnected += OnDeviceDisconnected;
            _controllerService.SamplingRateDetected += OnSamplingRateDetected;

            await _controllerService.StartAsync();

            App.Current?.Dispatcher.BeginInvoke(() =>
            {
                StatusMessage = "Controller connected";
            });
        }
        catch (Exception ex)
        {
            App.Current?.Dispatcher.BeginInvoke(() =>
            {
                StatusMessage = $"Error: {ex.Message}";
            });
        }
    }

    private async Task DisconnectControllerAsync()
    {
        if (_controllerService == null) return;

        _controllerService.InputReceived -= OnInputReceived;
        _controllerService.DeviceConnected -= OnDeviceConnected;
        _controllerService.DeviceDisconnected -= OnDeviceDisconnected;
        _controllerService.SamplingRateDetected -= OnSamplingRateDetected;

        await _controllerService.StopAsync();
        _controllerService.Dispose();
        _controllerService = null;

        IsControllerConnected = false;
        ControllerName = "No Controller";
        StatusMessage = "Controller disconnected";
    }

    private void OnInputReceived(object? sender, ControllerSnapshot snapshot)
    {
        _recordingService.OnInputReceived(snapshot);

        bool isTriggerActive = _recordingService.IsTriggerActive(snapshot);
        if (isTriggerActive && !_wasTriggerActive)
        {
            _recordingService.OnTriggerPressed(snapshot.TimestampTicks);
        }
        else if (!isTriggerActive && _wasTriggerActive)
        {
            _recordingService.OnTriggerReleased(snapshot.TimestampTicks);
        }
        _wasTriggerActive = isTriggerActive;

        long now = Stopwatch.GetTimestamp();
        if (now - _lastUiUpdateTicks < UiUpdateIntervalTicks) return;
        _lastUiUpdateTicks = now;

        if (_overlayWindow != null && IsOverlayVisible && _settingsService.Settings.OverlayConfig.ShowRealtime)
        {
            double ms = snapshot.TimestampTicks / (double)TimeSpan.TicksPerMillisecond;
            _overlayWindow.UpdateRealtimePoints(snapshot.LeftStick, snapshot.RightStick, ms);
        }

        App.Current?.Dispatcher.BeginInvoke(() =>
        {
            CurrentLeftStick = snapshot.LeftStick;
            CurrentRightStick = snapshot.RightStick;
            CurrentTriggers = snapshot.Triggers;
            MonitorView.UpdateLiveData(snapshot);
        });
    }

    private void OnDeviceConnected(object? sender, ControllerDeviceInfo device)
    {
        App.Current?.Dispatcher.BeginInvoke(() =>
        {
            IsControllerConnected = true;
            ControllerName = device.Name;
        });
    }

    private void OnDeviceDisconnected(object? sender, ControllerDeviceInfo device)
    {
        App.Current?.Dispatcher.BeginInvoke(async () =>
        {
            IsControllerConnected = false;
            ControllerName = "Disconnected";
            StatusMessage = "Controller disconnected, waiting for reconnect...";
            await DisconnectControllerAsync();
        });
    }

    private void OnSamplingRateDetected(object? sender, int rate)
    {
        App.Current?.Dispatcher.BeginInvoke(() =>
        {
            SamplingRate = rate;
            if (_settingsService.Settings.AutoDetectSamplingRate)
            {
                _recordingService.UpdateBufferSize(rate);
            }
        });
    }

    private async void OnRecordingCompleted(object? sender, TrackRecord record)
    {
        try
        {
            await _databaseService.SaveTrackRecordAsync(record);
            App.Current?.Dispatcher.BeginInvoke(() =>
            {
                RecordCount++;
                StatusMessage = $"Recording saved ({record.SampleCount} samples)";
                RecordsView.AddRecord(record);

                if (_overlayWindow != null && IsOverlayVisible)
                {
                    if (_settingsService.Settings.OverlayConfig.ShowHistory)
                        _overlayWindow.SetHistoryRecord(record);
                    if (_settingsService.Settings.OverlayConfig.ShowRecoilAnalysis)
                        ShowRecoilOnOverlay(record);
                }
            });
        }
        catch (Exception ex)
        {
            App.Current?.Dispatcher.BeginInvoke(() =>
            {
                StatusMessage = $"Save error: {ex.Message}";
            });
        }
    }

    private void OnRecordingStateChanged(object? sender, RecordingState state)
    {
        App.Current?.Dispatcher.BeginInvoke(() =>
        {
            RecordingState = state;
        });
    }

    public async Task ShutdownAsync()
    {
        StopAutoDetect();
        await DisconnectControllerAsync();

        if (_overlayWindow != null)
        {
            _overlayWindow.SavePosition(_settingsService.Settings.OverlayConfig);
            _overlayWindow.Close();
            _overlayWindow = null;
        }

        if (_imageOverlayWindow != null)
        {
            _imageOverlayWindow.SavePosition(_settingsService.Settings.ImageOverlayConfig);
            _imageOverlayWindow.Close();
            _imageOverlayWindow = null;
        }

        await _settingsService.SaveAsync();
        _databaseService.Dispose();
        _recordingService.Dispose();
    }
}
