using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CCWaterControllerPlayer.ViewModels;

namespace CCWaterControllerPlayer.Views;

public partial class TimeSeriesControl : UserControl
{
    public static readonly DependencyProperty DataPointsProperty =
        DependencyProperty.Register(nameof(DataPoints), typeof(ObservableCollection<TimeSeriesPoint>),
            typeof(TimeSeriesControl),
            new PropertyMetadata(null, OnDataPointsChanged));

    public static readonly DependencyProperty LineColorProperty =
        DependencyProperty.Register(nameof(LineColor), typeof(string),
            typeof(TimeSeriesControl),
            new PropertyMetadata("#FF6C63FF"));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string),
            typeof(TimeSeriesControl),
            new PropertyMetadata("", OnLabelChanged));

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double),
            typeof(TimeSeriesControl),
            new PropertyMetadata(0.0, OnProgressChanged));

    public static readonly DependencyProperty TimeWindowMsProperty =
        DependencyProperty.Register(nameof(TimeWindowMs), typeof(double),
            typeof(TimeSeriesControl),
            new PropertyMetadata(3000.0));

    public ObservableCollection<TimeSeriesPoint>? DataPoints
    {
        get => (ObservableCollection<TimeSeriesPoint>?)GetValue(DataPointsProperty);
        set => SetValue(DataPointsProperty, value);
    }

    public string LineColor
    {
        get => (string)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double TimeWindowMs
    {
        get => (double)GetValue(TimeWindowMsProperty);
        set => SetValue(TimeWindowMsProperty, value);
    }

    private Polyline? _polyline;
    private Line? _progressLine;
    private Line? _zeroLine;

    public TimeSeriesControl()
    {
        InitializeComponent();
    }

    private static void OnDataPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TimeSeriesControl)d;
        if (e.OldValue is ObservableCollection<TimeSeriesPoint> oldCol)
            oldCol.CollectionChanged -= control.OnCollectionChanged;
        if (e.NewValue is ObservableCollection<TimeSeriesPoint> newCol)
            newCol.CollectionChanged += control.OnCollectionChanged;
        control.Redraw();
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TimeSeriesControl)d;
        control.LabelText.Text = e.NewValue?.ToString() ?? "";
    }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TimeSeriesControl)d;
        control.UpdateProgressLine();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && _polyline != null)
        {
            UpdatePolylineFromData();
        }
        else
        {
            Redraw();
        }
    }

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Redraw();
    }

    private void Redraw()
    {
        ChartCanvas.Children.Clear();
        _polyline = null;
        _progressLine = null;
        _zeroLine = null;

        double w = ChartCanvas.ActualWidth;
        double h = ChartCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var zeroBrush = new SolidColorBrush(Color.FromArgb(40, 139, 148, 158));
        _zeroLine = new Line { X1 = 0, Y1 = h / 2, X2 = w, Y2 = h / 2, Stroke = zeroBrush, StrokeThickness = 1 };
        ChartCanvas.Children.Add(_zeroLine);

        var color = (Color)ColorConverter.ConvertFromString(LineColor);
        _polyline = new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Round
        };
        ChartCanvas.Children.Add(_polyline);

        _progressLine = new Line
        {
            Y1 = 0, Y2 = h,
            Stroke = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
            StrokeThickness = 1.5,
            Visibility = Visibility.Collapsed
        };
        ChartCanvas.Children.Add(_progressLine);

        UpdatePolylineFromData();
        UpdateProgressLine();
    }

    private void UpdatePolylineFromData()
    {
        if (_polyline == null || DataPoints == null || DataPoints.Count < 2) return;

        double w = ChartCanvas.ActualWidth;
        double h = ChartCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        double windowMs = TimeWindowMs;
        var points = DataPoints;
        double latestTime = points[^1].TimestampMs;
        double windowStart = latestTime - windowMs;

        double pixelsPerMs = w / windowMs;

        _polyline.Points.Clear();

        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            if (point.TimestampMs < windowStart) continue;

            double px = (point.TimestampMs - windowStart) * pixelsPerMs;
            double py = (1 - (point.Value + 1) / 2) * h;
            _polyline.Points.Add(new Point(px, py));
        }
    }

    private void UpdateProgressLine()
    {
        if (_progressLine == null) return;

        double w = ChartCanvas.ActualWidth;
        double h = ChartCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        if (Progress <= 0 || DataPoints == null || DataPoints.Count < 2)
        {
            _progressLine.Visibility = Visibility.Collapsed;
            return;
        }

        double windowMs = TimeWindowMs;
        double latestTime = DataPoints[^1].TimestampMs;
        double windowStart = latestTime - windowMs;
        double pixelsPerMs = w / windowMs;

        double totalDuration = latestTime - DataPoints[0].TimestampMs;
        if (totalDuration <= 0)
        {
            _progressLine.Visibility = Visibility.Collapsed;
            return;
        }

        double progressTime = DataPoints[0].TimestampMs + (Progress / 100.0) * totalDuration;

        if (progressTime < windowStart || progressTime > latestTime)
        {
            _progressLine.Visibility = Visibility.Collapsed;
        }
        else
        {
            _progressLine.Visibility = Visibility.Visible;
            double progressX = (progressTime - windowStart) * pixelsPerMs;
            _progressLine.X1 = progressX;
            _progressLine.X2 = progressX;
            _progressLine.Y1 = 0;
            _progressLine.Y2 = h;
        }
    }
}
