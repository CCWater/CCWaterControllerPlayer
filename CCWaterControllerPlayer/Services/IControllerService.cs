using CCWaterControllerPlayer.Models;

namespace CCWaterControllerPlayer.Services;

public interface IControllerService : IDisposable
{
    event EventHandler<ControllerSnapshot>? InputReceived;
    event EventHandler<ControllerDeviceInfo>? DeviceConnected;
    event EventHandler<ControllerDeviceInfo>? DeviceDisconnected;
    event EventHandler<int>? SamplingRateDetected;

    ControllerDeviceInfo? CurrentDevice { get; }
    bool IsRunning { get; }
    int DetectedSamplingRate { get; }
    int TargetPollingRateHz { get; set; }
    SamplingPerformance Performance { get; set; }

    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
    List<ControllerDeviceInfo> EnumerateDevices();
}
