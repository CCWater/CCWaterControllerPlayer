using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CCWaterControllerPlayer.Models;
using CCWaterControllerPlayer.Services;

namespace CCWaterControllerPlayer.ViewModels;

public class TriggerButtonOption
{
    public GamepadButton Value { get; }
    public string DisplayName { get; }

    public TriggerButtonOption(GamepadButton value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }
}

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private CancellationTokenSource? _saveDebounceCts;
    private bool _isLoading;
    public Action? OnSettingsSaved;

    public List<TriggerButtonOption> AvailableTriggerButtons { get; } = new()
    {
        new(GamepadButton.RightTrigger, "RT (Right Trigger)"),
        new(GamepadButton.LeftTrigger, "LT (Left Trigger)"),
        new(GamepadButton.RightShoulder, "RB (Right Bumper)"),
        new(GamepadButton.LeftShoulder, "LB (Left Bumper)"),
        new(GamepadButton.A, "A"),
        new(GamepadButton.B, "B"),
        new(GamepadButton.X, "X"),
        new(GamepadButton.Y, "Y"),
        new(GamepadButton.RightThumb, "RS (Right Stick)"),
        new(GamepadButton.LeftThumb, "LS (Left Stick)"),
        new(GamepadButton.DPadUp, "D-Pad Up"),
        new(GamepadButton.DPadDown, "D-Pad Down"),
        new(GamepadButton.DPadLeft, "D-Pad Left"),
        new(GamepadButton.DPadRight, "D-Pad Right"),
        new(GamepadButton.Start, "Start / Menu"),
        new(GamepadButton.Back, "Back / View"),
    };

    [ObservableProperty]
    private TriggerButtonOption? _selectedTriggerButton;

    [ObservableProperty]
    private SamplingPerformance _samplingPerformance;

    public bool IsPerformanceLow
    {
        get => SamplingPerformance == SamplingPerformance.Low;
        set { if (value) SamplingPerformance = SamplingPerformance.Low; }
    }

    public bool IsPerformanceMedium
    {
        get => SamplingPerformance == SamplingPerformance.Medium;
        set { if (value) SamplingPerformance = SamplingPerformance.Medium; }
    }

    public bool IsPerformanceHigh
    {
        get => SamplingPerformance == SamplingPerformance.High;
        set { if (value) SamplingPerformance = SamplingPerformance.High; }
    }

    public string SamplingPerformanceDescription => SamplingPerformance switch
    {
        SamplingPerformance.Low => LocalizationService.Instance.Strings.SamplingPerfLowDesc,
        SamplingPerformance.Medium => LocalizationService.Instance.Strings.SamplingPerfMediumDesc,
        SamplingPerformance.High => LocalizationService.Instance.Strings.SamplingPerfHighDesc,
        _ => string.Empty
    };

    [ObservableProperty]
    private int _samplingRateHz;

    [ObservableProperty]
    private bool _autoDetectSamplingRate;

    [ObservableProperty]
    private int _preTriggerMs;

    [ObservableProperty]
    private int _postTriggerMs;

    [ObservableProperty]
    private bool _mergeEnabled;

    [ObservableProperty]
    private int _mergeWindowMs;

    [ObservableProperty]
    private float _triggerThreshold;

    [ObservableProperty]
    private TriggerMode _triggerMode;

    [ObservableProperty]
    private GamepadButton _triggerButton;

    [ObservableProperty]
    private StickSide _defaultTrackedStick;

    [ObservableProperty]
    private string _selectedLanguage = string.Empty;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadFromSettings();
        PropertyChanged += OnPropertyAutoSave;
    }

    partial void OnSamplingPerformanceChanged(SamplingPerformance value)
    {
        OnPropertyChanged(nameof(IsPerformanceLow));
        OnPropertyChanged(nameof(IsPerformanceMedium));
        OnPropertyChanged(nameof(IsPerformanceHigh));
        OnPropertyChanged(nameof(SamplingPerformanceDescription));
    }

    private void OnPropertyAutoSave(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isLoading) return;
        if (e.PropertyName is nameof(SelectedTriggerButton) or nameof(SamplingPerformance) or nameof(SamplingRateHz) or nameof(AutoDetectSamplingRate)
            or nameof(PreTriggerMs) or nameof(PostTriggerMs) or nameof(MergeEnabled) or nameof(MergeWindowMs)
            or nameof(TriggerThreshold) or nameof(TriggerMode) or nameof(DefaultTrackedStick))
        {
            ScheduleAutoSave();
        }
    }

    private void ScheduleAutoSave()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts = new CancellationTokenSource();
        var token = _saveDebounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, token);
                if (!token.IsCancellationRequested)
                    await SaveSettingsAsync();
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private void LoadFromSettings()
    {
        _isLoading = true;
        var s = _settingsService.Settings;
        SamplingPerformance = s.SamplingPerformance;
        SamplingRateHz = s.SamplingRateHz;
        AutoDetectSamplingRate = s.AutoDetectSamplingRate;
        PreTriggerMs = s.TriggerConfig.PreTriggerMs;
        PostTriggerMs = s.TriggerConfig.PostTriggerMs;
        MergeEnabled = s.TriggerConfig.MergeEnabled;
        MergeWindowMs = s.TriggerConfig.MergeWindowMs;
        TriggerThreshold = s.TriggerConfig.TriggerThreshold;
        TriggerMode = s.TriggerConfig.Mode;
        TriggerButton = s.TriggerConfig.TriggerButton;
        DefaultTrackedStick = s.DefaultTrackedStick;
        SelectedLanguage = LocalizationService.Instance.CurrentLanguage;
        SelectedTriggerButton = AvailableTriggerButtons.FirstOrDefault(b => b.Value == TriggerButton)
                                ?? AvailableTriggerButtons[0];
        _isLoading = false;
    }

    [RelayCommand]
    private void SwitchLanguage(string langCode)
    {
        SelectedLanguage = langCode;
        LocalizationService.Instance.SetLanguage(langCode);
        _settingsService.Settings.Language = langCode;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var s = _settingsService.Settings;
        s.SamplingPerformance = SamplingPerformance;
        s.SamplingRateHz = AppSettings.GetSamplingRateForPerformance(SamplingPerformance);
        s.AutoDetectSamplingRate = AutoDetectSamplingRate;
        s.TriggerConfig.PreTriggerMs = PreTriggerMs;
        s.TriggerConfig.PostTriggerMs = PostTriggerMs;
        s.TriggerConfig.MergeEnabled = MergeEnabled;
        s.TriggerConfig.MergeWindowMs = MergeWindowMs;
        s.TriggerConfig.TriggerThreshold = TriggerThreshold;
        s.TriggerConfig.Mode = TriggerMode;
        s.TriggerConfig.TriggerButton = SelectedTriggerButton?.Value ?? GamepadButton.RightTrigger;
        s.DefaultTrackedStick = DefaultTrackedStick;
        s.Language = SelectedLanguage;

        await _settingsService.SaveAsync();
        OnSettingsSaved?.Invoke();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _settingsService.Settings = new AppSettings();
        LoadFromSettings();
    }
}
