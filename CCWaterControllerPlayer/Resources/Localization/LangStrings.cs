namespace CCWaterControllerPlayer.Resources.Localization;

public abstract class LangStrings
{
    public abstract string AppTitle { get; }
    public abstract string AppSubtitle { get; }

    public abstract string NavMonitor { get; }
    public abstract string NavRecords { get; }
    public abstract string NavSettings { get; }
    public abstract string NavHelp { get; }

    public abstract string MonitorTitle { get; }
    public abstract string LeftStick2D { get; }
    public abstract string RightStick2D { get; }
    public abstract string LeftStickTimeSeries { get; }
    public abstract string RightStickTimeSeries { get; }
    public abstract string Magnitude { get; }

    public abstract string StatusIdle { get; }
    public abstract string StatusRecording { get; }
    public abstract string StatusPostRecording { get; }
    public abstract string StatusReady { get; }
    public abstract string StatusNoController { get; }
    public abstract string StatusConnected { get; }
    public abstract string StatusDisconnected { get; }
    public abstract string StatusControllerNotFound { get; }

    public abstract string BtnConnect { get; }
    public abstract string BtnDisconnect { get; }
    public abstract string BtnCompare { get; }
    public abstract string BtnClear { get; }
    public abstract string BtnPlay { get; }
    public abstract string BtnPause { get; }
    public abstract string BtnStop { get; }
    public abstract string BtnSave { get; }
    public abstract string BtnReset { get; }

    public abstract string RecordsTitle { get; }
    public abstract string RecordsPoints { get; }

    public abstract string SettingsTitle { get; }
    public abstract string SettingsRecording { get; }
    public abstract string SettingsSampling { get; }
    public abstract string SettingsOverlay { get; }
    public abstract string SettingsLanguage { get; }
    public abstract string SettingsPreTrigger { get; }
    public abstract string SettingsPostTrigger { get; }
    public abstract string SettingsTriggerButton { get; }
    public abstract string SettingsTriggerThreshold { get; }
    public abstract string SettingsMergeConsecutive { get; }
    public abstract string SettingsMergeWindow { get; }
    public abstract string SettingsAutoDetectRate { get; }
    public abstract string SettingsTargetRate { get; }
    public abstract string SettingsEnableOverlay { get; }
    public abstract string SettingsOverlayWidth { get; }
    public abstract string SettingsOverlayHeight { get; }
    public abstract string SettingsOverlayOpacity { get; }
    public abstract string SettingsShowRealtime { get; }
    public abstract string SettingsShowHistory { get; }
    public abstract string SettingsShowRecoil { get; }
    public abstract string SettingsResetDefaults { get; }
    public abstract string SettingsSaveSettings { get; }

    public abstract string LangChinese { get; }
    public abstract string LangEnglish { get; }

    public abstract string HelpTitle { get; }
    public abstract string HelpQuickStart { get; }
    public abstract string HelpFeatures { get; }
    public abstract string HelpRecording { get; }
    public abstract string HelpVisualization { get; }
    public abstract string HelpOverlay { get; }
    public abstract string HelpFAQ { get; }

    public abstract string BtnFullscreen { get; }
    public abstract string BtnFullscreenBoth { get; }
    public abstract string BtnFullscreenPlayback { get; }

    public abstract string AnalysisTitle { get; }
    public abstract string AnalysisInputLabel { get; }
    public abstract string AnalysisRecoilLabel { get; }

    public abstract string BtnDeleteTemp { get; }
    public abstract string StatusTemporary { get; }
    public abstract string StatusPermanent { get; }

    public abstract string ImageOverlayTitle { get; }
    public abstract string ImageOverlayImage { get; }

    public abstract string Rate { get; }
    public abstract string Rec { get; }
}
