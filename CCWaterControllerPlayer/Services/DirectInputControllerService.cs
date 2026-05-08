using System.Diagnostics;
using CCWaterControllerPlayer.Models;
using SharpDX.DirectInput;

namespace CCWaterControllerPlayer.Services;

public class DirectInputControllerService : IControllerService
{
    private DirectInput? _directInput;
    private Joystick? _joystick;
    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private readonly Stopwatch _stopwatch = new();
    private int _detectedSamplingRate;
    private bool _isRunning;
    private ControllerDeviceInfo? _currentDevice;

    public event EventHandler<ControllerSnapshot>? InputReceived;
    public event EventHandler<ControllerDeviceInfo>? DeviceConnected;
    public event EventHandler<ControllerDeviceInfo>? DeviceDisconnected;
    public event EventHandler<int>? SamplingRateDetected;

    public ControllerDeviceInfo? CurrentDevice => _currentDevice;
    public bool IsRunning => _isRunning;
    public int DetectedSamplingRate => _detectedSamplingRate;

    public int TargetPollingRateHz { get; set; } = 1000;

    public List<ControllerDeviceInfo> EnumerateDevices()
    {
        var devices = new List<ControllerDeviceInfo>();
        using var di = new DirectInput();

        foreach (var deviceInstance in di.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
        {
            devices.Add(new ControllerDeviceInfo
            {
                Id = deviceInstance.InstanceGuid.ToString(),
                Name = deviceInstance.InstanceName,
                Type = DetectControllerType(deviceInstance.InstanceName),
                IsConnected = true
            });
        }

        foreach (var deviceInstance in di.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
        {
            devices.Add(new ControllerDeviceInfo
            {
                Id = deviceInstance.InstanceGuid.ToString(),
                Name = deviceInstance.InstanceName,
                Type = DetectControllerType(deviceInstance.InstanceName),
                IsConnected = true
            });
        }

        return devices;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return Task.CompletedTask;

        _directInput = new DirectInput();
        var device = FindDevice(_directInput);
        if (device == null)
            throw new InvalidOperationException("No DirectInput controller found");

        _joystick = new Joystick(_directInput, device.Value);
        _joystick.Properties.BufferSize = 128;
        _joystick.Acquire();

        _currentDevice = new ControllerDeviceInfo
        {
            Id = device.Value.ToString(),
            Name = _joystick.Information.InstanceName,
            Type = DetectControllerType(_joystick.Information.InstanceName),
            IsConnected = true
        };

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isRunning = true;
        _stopwatch.Restart();

        DeviceConnected?.Invoke(this, _currentDevice);

        _pollingTask = Task.Run(() => PollingLoop(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;

        _cts?.Cancel();
        if (_pollingTask != null)
        {
            try { await _pollingTask; } catch (OperationCanceledException) { }
        }

        _isRunning = false;
        _stopwatch.Stop();

        _joystick?.Unacquire();
        _joystick?.Dispose();
        _directInput?.Dispose();

        if (_currentDevice != null)
        {
            _currentDevice.IsConnected = false;
            DeviceDisconnected?.Invoke(this, _currentDevice);
        }
    }

    private async Task PollingLoop(CancellationToken ct)
    {
        int sampleCount = 0;
        long lastRateCheckTicks = _stopwatch.ElapsedTicks;
        int rate = Math.Clamp(TargetPollingRateHz, 100, 2000);
        double intervalMs = 1000.0 / rate;

        while (!ct.IsCancellationRequested)
        {
            long loopStart = _stopwatch.ElapsedTicks;

            if (_joystick == null) break;

            try
            {
                _joystick.Poll();
                var state = _joystick.GetCurrentState();

                var snapshot = new ControllerSnapshot
                {
                    TimestampTicks = (long)(_stopwatch.ElapsedTicks * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)),
                    LeftStick = new StickPosition(
                        NormalizeDirectInputAxis(state.X),
                        NormalizeDirectInputAxis(state.Y, true)),
                    RightStick = new StickPosition(
                        NormalizeDirectInputAxis(state.RotationX),
                        NormalizeDirectInputAxis(state.RotationY, true)),
                    Triggers = new TriggerState
                    {
                        Left = state.Z > 32767 ? (state.Z - 32767) / 32767f : 0f,
                        Right = state.Z < 32767 ? (32767 - state.Z) / 32767f : 0f
                    },
                    Buttons = ButtonsToUint(state.Buttons)
                };

                InputReceived?.Invoke(this, snapshot);
                sampleCount++;
            }
            catch (SharpDX.SharpDXException)
            {
                await Task.Delay(1000, ct);
                try { _joystick?.Acquire(); } catch { }
                continue;
            }

            long elapsed = _stopwatch.ElapsedTicks - lastRateCheckTicks;
            if (elapsed >= Stopwatch.Frequency)
            {
                _detectedSamplingRate = sampleCount;
                if (_currentDevice != null)
                    _currentDevice.PollingRateHz = _detectedSamplingRate;
                SamplingRateDetected?.Invoke(this, _detectedSamplingRate);
                sampleCount = 0;
                lastRateCheckTicks = _stopwatch.ElapsedTicks;
            }

            double elapsedMs = (_stopwatch.ElapsedTicks - loopStart) * 1000.0 / Stopwatch.Frequency;
            double sleepMs = intervalMs - elapsedMs;
            if (sleepMs >= 1.0)
            {
                await Task.Delay((int)sleepMs, ct);
            }
            else
            {
                await Task.Delay(1, ct);
            }
        }
    }

    private static float NormalizeDirectInputAxis(int value, bool invert = false)
    {
        float normalized = (value - 32767f) / 32767f;
        return invert ? -normalized : normalized;
    }

    private static uint ButtonsToUint(bool[] buttons)
    {
        uint result = 0;
        for (int i = 0; i < Math.Min(buttons.Length, 32); i++)
        {
            if (buttons[i])
                result |= (uint)(1 << i);
        }
        return result;
    }

    private static Guid? FindDevice(DirectInput di)
    {
        foreach (var device in di.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
            return device.InstanceGuid;
        foreach (var device in di.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
            return device.InstanceGuid;
        return null;
    }

    private static ControllerType DetectControllerType(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("xbox") || lower.Contains("xinput"))
            return ControllerType.Xbox;
        if (lower.Contains("playstation") || lower.Contains("dualshock") || lower.Contains("dualsense") || lower.Contains("ps"))
            return ControllerType.PlayStation;
        return ControllerType.Unknown;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
    }
}
