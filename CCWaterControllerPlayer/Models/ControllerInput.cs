namespace CCWaterControllerPlayer.Models;

public enum ControllerType
{
    Xbox,
    PlayStation,
    Unknown
}

public enum StickSide
{
    Left,
    Right
}

public struct StickPosition
{
    public float X { get; set; }
    public float Y { get; set; }

    public StickPosition(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float Magnitude => MathF.Sqrt(X * X + Y * Y);
    public float Angle => MathF.Atan2(Y, X);
}

public struct TriggerState
{
    public float Left { get; set; }
    public float Right { get; set; }
}

public class ControllerSnapshot
{
    public long TimestampTicks { get; set; }
    public StickPosition LeftStick { get; set; }
    public StickPosition RightStick { get; set; }
    public TriggerState Triggers { get; set; }
    public uint Buttons { get; set; }

    public double TimestampMs => TimestampTicks / (double)TimeSpan.TicksPerMillisecond;
}

public class ControllerDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ControllerType Type { get; set; }
    public bool IsConnected { get; set; }
    public int PollingRateHz { get; set; }
}
