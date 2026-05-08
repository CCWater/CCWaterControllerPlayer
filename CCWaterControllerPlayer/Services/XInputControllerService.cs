using System.Diagnostics;
using CCWaterControllerPlayer.Models;
using SharpDX.XInput;

namespace CCWaterControllerPlayer.Services;

public class XInputControllerService : IControllerService
{
    private Controller? _controller;
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
    public bool AutoDetect { get; set; } = true;

    public List<ControllerDeviceInfo> EnumerateDevices()
    {
        var devices = new List<ControllerDeviceInfo>();
        for (int i = 0; i < 4; i++)
        {
            var controller = new Controller((UserIndex)i);
            if (controller.IsConnected)
            {
                devices.Add(new ControllerDeviceInfo
                {
                    Id = $"XInput_{i}",
                    Name = $"Xbox Controller {i + 1}",
                    Type = ControllerType.Xbox,
                    IsConnected = true
                });
            }
        }
        return devices;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return Task.CompletedTask;

        _controller = FindController();
        if (_controller == null)
            throw new InvalidOperationException("No Xbox controller found");

        _currentDevice = new ControllerDeviceInfo
        {
            Id = $"XInput_{(int)_controller.UserIndex}",
            Name = $"Xbox Controller {(int)_controller.UserIndex + 1}",
            Type = ControllerType.Xbox,
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

            if (_controller == null || !_controller.IsConnected)
            {
                _controller = FindController();
                if (_controller == null)
                {
                    await Task.Delay(500, ct);
                    continue;
                }
            }

            var state = _controller.GetState();
            var gamepad = state.Gamepad;

            var snapshot = new ControllerSnapshot
            {
                TimestampTicks = (long)(_stopwatch.ElapsedTicks * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)),
                LeftStick = new StickPosition(
                    NormalizeAxis(gamepad.LeftThumbX),
                    NormalizeAxis(gamepad.LeftThumbY)),
                RightStick = new StickPosition(
                    NormalizeAxis(gamepad.RightThumbX),
                    NormalizeAxis(gamepad.RightThumbY)),
                Triggers = new TriggerState
                {
                    Left = gamepad.LeftTrigger / 255f,
                    Right = gamepad.RightTrigger / 255f
                },
                Buttons = (uint)gamepad.Buttons
            };

            InputReceived?.Invoke(this, snapshot);
            sampleCount++;

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

    private static float NormalizeAxis(short value)
    {
        return value / 32767f;
    }

    private static Controller? FindController()
    {
        for (int i = 0; i < 4; i++)
        {
            var controller = new Controller((UserIndex)i);
            if (controller.IsConnected)
                return controller;
        }
        return null;
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
    }
}
