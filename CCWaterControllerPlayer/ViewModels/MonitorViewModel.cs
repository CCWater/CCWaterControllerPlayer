using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CCWaterControllerPlayer.Models;
using CCWaterControllerPlayer.Services;

namespace CCWaterControllerPlayer.ViewModels;

public partial class MonitorViewModel : ViewModelBase
{
    private readonly MainViewModel _mainViewModel;
    private const double TrimWindowMs = 4000;

    [ObservableProperty]
    private ObservableCollection<TrackPoint> _leftStickTrackPoints = new();

    [ObservableProperty]
    private ObservableCollection<TrackPoint> _rightStickTrackPoints = new();

    [ObservableProperty]
    private ObservableCollection<TimeSeriesPoint> _leftStickTimeSeriesX = new();

    [ObservableProperty]
    private ObservableCollection<TimeSeriesPoint> _leftStickTimeSeriesY = new();

    [ObservableProperty]
    private ObservableCollection<TimeSeriesPoint> _rightStickTimeSeriesX = new();

    [ObservableProperty]
    private ObservableCollection<TimeSeriesPoint> _rightStickTimeSeriesY = new();

    [ObservableProperty]
    private float _leftX;

    [ObservableProperty]
    private float _leftY;

    [ObservableProperty]
    private float _leftMagnitude;

    [ObservableProperty]
    private float _rightX;

    [ObservableProperty]
    private float _rightY;

    [ObservableProperty]
    private float _rightMagnitude;

    [ObservableProperty]
    private StickSide _activeStickView = StickSide.Right;

    public MainViewModel MainViewModel => _mainViewModel;

    public MonitorViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    private void SwitchStickView(StickSide side)
    {
        ActiveStickView = side;
    }

    private const int MaxDisplayPoints = 500;

    public void UpdateLiveData(ControllerSnapshot snapshot)
    {
        var left = snapshot.LeftStick;
        var right = snapshot.RightStick;

        LeftX = left.X;
        LeftY = left.Y;
        LeftMagnitude = left.Magnitude;

        RightX = right.X;
        RightY = right.Y;
        RightMagnitude = right.Magnitude;

        double nowMs = snapshot.TimestampMs;
        double trimThreshold = nowMs - TrimWindowMs;

        LeftStickTrackPoints.Add(new TrackPoint(left.X, left.Y, nowMs));
        RightStickTrackPoints.Add(new TrackPoint(right.X, right.Y, nowMs));
        LeftStickTimeSeriesX.Add(new TimeSeriesPoint(nowMs, left.X));
        LeftStickTimeSeriesY.Add(new TimeSeriesPoint(nowMs, left.Y));
        RightStickTimeSeriesX.Add(new TimeSeriesPoint(nowMs, right.X));
        RightStickTimeSeriesY.Add(new TimeSeriesPoint(nowMs, right.Y));

        TrimCollection(LeftStickTrackPoints, trimThreshold, p => p.TimestampMs);
        TrimCollection(RightStickTrackPoints, trimThreshold, p => p.TimestampMs);
        TrimCollection(LeftStickTimeSeriesX, trimThreshold, p => p.TimestampMs);
        TrimCollection(LeftStickTimeSeriesY, trimThreshold, p => p.TimestampMs);
        TrimCollection(RightStickTimeSeriesX, trimThreshold, p => p.TimestampMs);
        TrimCollection(RightStickTimeSeriesY, trimThreshold, p => p.TimestampMs);
    }

    private static void TrimCollection<T>(ObservableCollection<T> collection, double threshold, Func<T, double> getTime)
    {
        int removeCount = 0;
        while (removeCount < collection.Count && getTime(collection[removeCount]) < threshold)
            removeCount++;

        if (removeCount > MaxDisplayPoints / 2)
        {
            for (int i = 0; i < removeCount; i++)
                collection.RemoveAt(0);
        }
        else if (collection.Count > MaxDisplayPoints)
        {
            int excess = collection.Count - MaxDisplayPoints;
            for (int i = 0; i < excess; i++)
                collection.RemoveAt(0);
        }
    }
}

public record TrackPoint(float X, float Y, double TimestampMs);
public record TimeSeriesPoint(double TimestampMs, float Value);
