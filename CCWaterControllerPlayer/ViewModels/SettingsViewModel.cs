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
    }

    private void LoadFromSettings()
    {
        var s = _settingsService.Settings;
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
        s.SamplingRateHz = SamplingRateHz;
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
