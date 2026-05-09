namespace CCWaterControllerPlayer.Resources.Localization;

public class LangStrings_EN : LangStrings
{
    public override string AppTitle => "CCWater Controller Player";
    public override string AppSubtitle => "Controller Track Analyzer";

    public override string NavMonitor => "📊 Monitor";
    public override string NavRecords => "📁 Records";
    public override string NavSettings => "⚙️ Settings";
    public override string NavHelp => "❓ Help";

    public override string MonitorTitle => "Live Monitor";
    public override string LeftStick2D => "Left Stick - 2D Track";
    public override string RightStick2D => "Right Stick - 2D Track";
    public override string LeftStickTimeSeries => "Left Stick - Time Series";
    public override string RightStickTimeSeries => "Right Stick - Time Series";
    public override string Magnitude => "Mag";

    public override string StatusIdle => "Idle";
    public override string StatusRecording => "Recording";
    public override string StatusPostRecording => "Post-Recording";
    public override string StatusReady => "Ready";
    public override string StatusNoController => "No Controller";
    public override string StatusConnected => "Controller connected";
    public override string StatusDisconnected => "Disconnected";
    public override string StatusControllerNotFound => "No controller found";

    public override string BtnConnect => "Connect";
    public override string BtnDisconnect => "Disconnect";
    public override string BtnCompare => "Compare";
    public override string BtnClear => "Clear";
    public override string BtnPlay => "▶ Play";
    public override string BtnPause => "⏸ Pause";
    public override string BtnStop => "⏹";
    public override string BtnSave => "Save";
    public override string BtnReset => "Reset";

    public override string RecordsTitle => "Track Records";
    public override string RecordsPoints => "pts";

    public override string SettingsTitle => "Settings";
    public override string SettingsRecording => "RECORDING";
    public override string SettingsSampling => "SAMPLING";
    public override string SettingsOverlay => "OVERLAY";
    public override string SettingsLanguage => "LANGUAGE";
    public override string SettingsPreTrigger => "Pre-trigger (ms)";
    public override string SettingsPostTrigger => "Post-trigger (ms)";
    public override string SettingsTriggerButton => "Trigger Button";
    public override string SettingsTriggerThreshold => "Trigger Threshold";
    public override string SettingsMergeConsecutive => "Merge consecutive triggers";
    public override string SettingsMergeWindow => "Merge Window (ms)";
    public override string SettingsAutoDetectRate => "Auto-detect sampling rate";
    public override string SettingsTargetRate => "Target Rate (Hz)";
    public override string SettingsEnableOverlay => "Enable Overlay";
    public override string SettingsOverlayWidth => "Width";
    public override string SettingsOverlayHeight => "Height";
    public override string SettingsOverlayOpacity => "Opacity";
    public override string SettingsShowRealtime => "Show realtime track";
    public override string SettingsShowHistory => "Show history overlay";
    public override string SettingsShowRecoil => "Show recoil analysis";
    public override string SettingsResetDefaults => "Reset Defaults";
    public override string SettingsSaveSettings => "Save Settings";

    public override string LangChinese => "中文";
    public override string LangEnglish => "English";

    public override string HelpTitle => "Help & Documentation";
    public override string HelpQuickStart => "Quick Start";
    public override string HelpFeatures => "Features";
    public override string HelpRecording => "Recording";
    public override string HelpVisualization => "Visualization";
    public override string HelpOverlay => "Overlay";
    public override string HelpFAQ => "FAQ";

    public override string BtnFullscreen => "Fullscreen";
    public override string BtnFullscreenBoth => "Both Sticks Fullscreen";
    public override string BtnFullscreenPlayback => "Playback Fullscreen";

    public override string AnalysisTitle => "Recoil Trajectory";
    public override string AnalysisInputLabel => "Input";
    public override string AnalysisRecoilLabel => "Recoil";

    public override string BtnDeleteTemp => "🗑 Clear Temp";
    public override string StatusTemporary => "Temporary";
    public override string StatusPermanent => "Permanent";

    public override string ImageOverlayTitle => "IMAGE OVERLAY";
    public override string ImageOverlayImage => "Image Path";

    public override string Rate => "Rate";
    public override string Rec => "Rec";

    public override string TipToggleStatus => "Toggle Status";
    public override string TipSendToOverlay => "Send to Overlay";
    public override string TipCompare => "Compare";
    public override string TipRename => "Rename";
    public override string TipDelete => "Delete";
    public override string TipSelectImage => "Select Image File";

    public override string FirstRunTitle => "Welcome";
    public override string FirstRunMessage => "No fire button (recording trigger) has been configured yet. Would you like to go to Settings now?";
    public override string FirstRunGoSettings => "Go to Settings";
    public override string FirstRunSkip => "Later";

    public override string SettingsSamplingPerformance => "Sampling Performance";
    public override string SamplingPerfLow => "Low (50Hz)";
    public override string SamplingPerfMedium => "Medium (1000Hz)";
    public override string SamplingPerfHigh => "High (8000Hz)";
    public override string SamplingPerfLowDesc => "Minimal CPU, suitable for track drawing";
    public override string SamplingPerfMediumDesc => "Moderate CPU, higher precision";
    public override string SamplingPerfHighDesc => "High CPU (saturates one core), maximum precision";
}
