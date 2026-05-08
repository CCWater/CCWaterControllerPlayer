namespace CCWaterControllerPlayer.Resources.Localization;

public class LangStrings_ZH : LangStrings
{
    public override string AppTitle => "CCWater Controller Player";
    public override string AppSubtitle => "手柄轨迹分析工具";

    public override string NavMonitor => "📊 实时监控";
    public override string NavRecords => "📁 录制记录";
    public override string NavSettings => "⚙️ 设置";
    public override string NavHelp => "❓ 帮助";

    public override string MonitorTitle => "实时监控";
    public override string LeftStick2D => "左摇杆 - 2D轨迹";
    public override string RightStick2D => "右摇杆 - 2D轨迹";
    public override string LeftStickTimeSeries => "左摇杆 - 时间序列";
    public override string RightStickTimeSeries => "右摇杆 - 时间序列";
    public override string Magnitude => "幅度";

    public override string StatusIdle => "空闲";
    public override string StatusRecording => "录制中";
    public override string StatusPostRecording => "后录中";
    public override string StatusReady => "就绪";
    public override string StatusNoController => "未连接手柄";
    public override string StatusConnected => "手柄已连接";
    public override string StatusDisconnected => "已断开";
    public override string StatusControllerNotFound => "未找到手柄";

    public override string BtnConnect => "连接";
    public override string BtnDisconnect => "断开";
    public override string BtnCompare => "对比";
    public override string BtnClear => "清除";
    public override string BtnPlay => "▶ 播放";
    public override string BtnPause => "⏸ 暂停";
    public override string BtnStop => "⏹";
    public override string BtnSave => "保存";
    public override string BtnReset => "重置";

    public override string RecordsTitle => "轨迹记录";
    public override string RecordsPoints => "采样点";

    public override string SettingsTitle => "设置";
    public override string SettingsRecording => "录制设置";
    public override string SettingsSampling => "采样设置";
    public override string SettingsOverlay => "悬浮窗设置";
    public override string SettingsLanguage => "语言设置";
    public override string SettingsPreTrigger => "触发前时长(ms)";
    public override string SettingsPostTrigger => "触发后时长(ms)";
    public override string SettingsTriggerButton => "触发按键";
    public override string SettingsTriggerThreshold => "触发阈值";
    public override string SettingsMergeConsecutive => "合并连续触发";
    public override string SettingsMergeWindow => "合并窗口 (ms)";
    public override string SettingsAutoDetectRate => "自动检测采样率";
    public override string SettingsTargetRate => "目标采样率(Hz)";
    public override string SettingsEnableOverlay => "启用悬浮窗";
    public override string SettingsOverlayWidth => "宽度";
    public override string SettingsOverlayHeight => "高度";
    public override string SettingsOverlayOpacity => "透明度";
    public override string SettingsShowRealtime => "显示实时轨迹";
    public override string SettingsShowHistory => "显示历史叠加";
    public override string SettingsShowRecoil => "显示后坐力分析";
    public override string SettingsResetDefaults => "恢复默认";
    public override string SettingsSaveSettings => "保存设置";

    public override string LangChinese => "中文";
    public override string LangEnglish => "English";

    public override string HelpTitle => "帮助与文档";
    public override string HelpQuickStart => "快速开始";
    public override string HelpFeatures => "功能介绍";
    public override string HelpRecording => "录制说明";
    public override string HelpVisualization => "可视化说明";
    public override string HelpOverlay => "悬浮窗说明";
    public override string HelpFAQ => "常见问题";

    public override string BtnFullscreen => "放大";
    public override string BtnFullscreenBoth => "左右摇杆放大";
    public override string BtnFullscreenPlayback => "播放面板放大";

    public override string AnalysisTitle => "后坐力轨迹分析";
    public override string AnalysisInputLabel => "输入轨迹";
    public override string AnalysisRecoilLabel => "后坐力轨迹";

    public override string BtnDeleteTemp => "🗑 清除临时";
    public override string StatusTemporary => "临时";
    public override string StatusPermanent => "永久";

    public override string ImageOverlayTitle => "图片悬浮窗";
    public override string ImageOverlayImage => "图片路径";

    public override string Rate => "采样率";
    public override string Rec => "录制";
}
