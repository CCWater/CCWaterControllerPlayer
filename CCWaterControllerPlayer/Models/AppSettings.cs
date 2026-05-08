namespace CCWaterControllerPlayer.Models;

public class RecordingTriggerConfig
{
    public TriggerMode Mode { get; set; } = TriggerMode.SingleButton;
    public GamepadButton TriggerButton { get; set; } = GamepadButton.RightTrigger;
    public GamepadButton[]? ComboButtons { get; set; }
    public float TriggerThreshold { get; set; } = 0.1f;
    public int PreTriggerMs { get; set; } = 500;
    public int PostTriggerMs { get; set; } = 500;
    public bool MergeEnabled { get; set; } = true;
    public int MergeWindowMs { get; set; } = 500;
}

public enum TriggerMode
{
    SingleButton,
    ComboButtons,
    ThresholdBased
}

public enum GamepadButton : uint
{
    DPadUp = 0x0001,
    DPadDown = 0x0002,
    DPadLeft = 0x0004,
    DPadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftThumb = 0x0040,
    RightThumb = 0x0080,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000,
    LeftTrigger = 0x10000,
    RightTrigger = 0x20000
}

public class OverlayConfig
{
    public bool Enabled { get; set; } = true;
    public OverlayMode Mode { get; set; } = OverlayMode.Transparent;
    public int Width { get; set; } = 600;
    public int Height { get; set; } = 320;
    public int PositionX { get; set; } = 50;
    public int PositionY { get; set; } = 50;
    public float Opacity { get; set; } = 0.8f;
    public bool ShowRealtime { get; set; } = true;
    public bool ShowHistory { get; set; } = true;
    public bool ShowRecoilAnalysis { get; set; } = true;
}

public enum OverlayMode
{
    Transparent,
    Independent
}

public class ImageOverlayConfig
{
    public bool Enabled { get; set; }
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 300;
    public int PositionX { get; set; } = 400;
    public int PositionY { get; set; } = 50;
    public float Opacity { get; set; } = 0.9f;
    public string ImagePath { get; set; } = string.Empty;
}

public class WindowState
{
    public double Left { get; set; } = double.NaN;
    public double Top { get; set; } = double.NaN;
    public double Width { get; set; } = 1100;
    public double Height { get; set; } = 720;
    public bool IsMaximized { get; set; }
}

public enum RecordStatus
{
    Temporary = 0,
    Permanent = 1
}

public class AppSettings
{
    public int SamplingRateHz { get; set; } = 8000;
    public bool AutoDetectSamplingRate { get; set; } = true;
    public RecordingTriggerConfig TriggerConfig { get; set; } = new();
    public OverlayConfig OverlayConfig { get; set; } = new();
    public ImageOverlayConfig ImageOverlayConfig { get; set; } = new();
    public StickSide DefaultTrackedStick { get; set; } = StickSide.Right;
    public int RingBufferSeconds { get; set; } = 5;
    public string Language { get; set; } = string.Empty;
    public WindowState MainWindowState { get; set; } = new();
    public bool HasConfiguredTrigger { get; set; }
}
