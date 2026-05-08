using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using CCWaterControllerPlayer.Models;
using CCWaterControllerPlayer.ViewModels;
using CCWaterControllerPlayer.Views;

namespace CCWaterControllerPlayer.Overlay;

public partial class OverlayWindow : Window
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int GWL_EXSTYLE = -20;
    private const double RealtimeWindowMs = 3000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private bool _isClickThrough;
    private bool _isPinned;

    private readonly ObservableCollection<TrackPoint> _leftRealtimePoints = new();
    private readonly ObservableCollection<TrackPoint> _rightRealtimePoints = new();
    private readonly ObservableCollection<List<TrackPoint>> _leftHistoryTracks = new();
    private readonly ObservableCollection<List<TrackPoint>> _rightHistoryTracks = new();
    private readonly DispatcherTimer _clockTimer;

    public bool IsPinned => _isPinned;
    public Action? OnPinStateChanged { get; set; }
    public Action? OnPositionChanged { get; set; }

    public OverlayWindow()
    {
        InitializeComponent();
        LeftStickVis.TrackPoints = _leftRealtimePoints;
        LeftStickVis.HistoryTracks = _leftHistoryTracks;
        RightStickVis.TrackPoints = _rightRealtimePoints;
        RightStickVis.HistoryTracks = _rightHistoryTracks;

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
        _clockTimer.Start();
        ClockText.Text = DateTime.Now.ToString("HH:mm:ss");

        LocationChanged += (_, _) => NotifyPositionChanged();
        SizeChanged += (_, _) => NotifyPositionChanged();
    }

    private void NotifyPositionChanged()
    {
        if (!_isPinned && IsLoaded)
            OnPositionChanged?.Invoke();
    }

    public void Configure(OverlayConfig config)
    {
        Width = config.Width;
        Height = config.Height;
        Left = config.PositionX;
        Top = config.PositionY;
        Opacity = config.Opacity;
        UpdatePanelVisibility(config.ShowRealtime, config.ShowRecoilAnalysis);
    }

    public void UpdatePanelVisibility(bool showRealtime, bool showRecoil)
    {
        LeftStickPanel.Visibility = showRealtime ? Visibility.Visible : Visibility.Collapsed;
        RightStickPanel.Visibility = showRealtime ? Visibility.Visible : Visibility.Collapsed;
        RecoilPanel.Visibility = showRecoil ? Visibility.Visible : Visibility.Collapsed;

        LeftStickColumn.Width = showRealtime ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        RightStickColumn.Width = showRealtime ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        RecoilColumn.Width = showRecoil ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
    }

    public void Pin()
    {
        _isPinned = true;
        TitleBar.Visibility = Visibility.Collapsed;
        SetClickThrough(true);
        OnPinStateChanged?.Invoke();
    }

    public void Unpin()
    {
        _isPinned = false;
        SetClickThrough(false);
        TitleBar.Visibility = Visibility.Visible;
        OnPinStateChanged?.Invoke();
    }

    public void SetClickThrough(bool enabled)
    {
        if (_isClickThrough == enabled) return;
        _isClickThrough = enabled;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        if (enabled)
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        else
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);

        InvalidateVisual();
    }

    public void UpdateRealtimePoints(StickPosition left, StickPosition right, double timestampMs)
    {
        Dispatcher.BeginInvoke(() =>
        {
            double cutoff = timestampMs - RealtimeWindowMs;

            while (_leftRealtimePoints.Count > 0 && _leftRealtimePoints[0].TimestampMs < cutoff)
                _leftRealtimePoints.RemoveAt(0);
            while (_rightRealtimePoints.Count > 0 && _rightRealtimePoints[0].TimestampMs < cutoff)
                _rightRealtimePoints.RemoveAt(0);

            _leftRealtimePoints.Add(new TrackPoint(left.X, left.Y, timestampMs));
            _rightRealtimePoints.Add(new TrackPoint(right.X, right.Y, timestampMs));

            LeftXText.Text = $"X:{left.X,6:F2}";
            LeftYText.Text = $"Y:{left.Y,6:F2}";
            RightXText.Text = $"X:{right.X,6:F2}";
            RightYText.Text = $"Y:{right.Y,6:F2}";
        });
    }

    public void SetHistoryRecord(TrackRecord record)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var leftTrack = new List<TrackPoint>();
            var rightTrack = new List<TrackPoint>();

            if (record.Snapshots.Count == 0) return;

            long baseTime = record.Snapshots[0].TimestampTicks;
            int step = Math.Max(1, record.Snapshots.Count / 500);

            for (int i = 0; i < record.Snapshots.Count; i += step)
            {
                var s = record.Snapshots[i];
                double ms = (s.TimestampTicks - baseTime) / (double)TimeSpan.TicksPerMillisecond;
                leftTrack.Add(new TrackPoint(s.LeftStick.X, s.LeftStick.Y, ms));
                rightTrack.Add(new TrackPoint(s.RightStick.X, s.RightStick.Y, ms));
            }

            _leftHistoryTracks.Clear();
            _rightHistoryTracks.Clear();
            _leftHistoryTracks.Add(leftTrack);
            _rightHistoryTracks.Add(rightTrack);
        });
    }

    public void SetRecoilData(ObservableCollection<RecoilPoint> inputTrack,
                              ObservableCollection<RecoilPoint> recoilTrack,
                              double triggerStartMs, double triggerEndMs)
    {
        Dispatcher.BeginInvoke(() =>
        {
            RecoilVis.InputTrack = inputTrack;
            RecoilVis.RecoilTrack = recoilTrack;
            RecoilVis.TriggerStartMs = triggerStartMs;
            RecoilVis.TriggerEndMs = triggerEndMs;
            RecoilVis.ShowInputTrack = true;
            RecoilVis.ShowRecoilTrack = true;
            RecoilVis.CurrentIndex = inputTrack.Count > 0 ? inputTrack.Count - 1 : -1;
        });
    }

    public void ClearRecoilData()
    {
        Dispatcher.BeginInvoke(() =>
        {
            RecoilVis.InputTrack = null;
            RecoilVis.RecoilTrack = null;
            RecoilVis.CurrentIndex = -1;
        });
    }

    public void ClearHistory()
    {
        Dispatcher.BeginInvoke(() =>
        {
            _leftHistoryTracks.Clear();
            _rightHistoryTracks.Clear();
        });
    }

    public void SavePosition(OverlayConfig config)
    {
        if (!_isPinned)
        {
            config.PositionX = (int)Left;
            config.PositionY = (int)Top;
            config.Width = (int)Width;
            config.Height = (int)Height;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        Pin();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
