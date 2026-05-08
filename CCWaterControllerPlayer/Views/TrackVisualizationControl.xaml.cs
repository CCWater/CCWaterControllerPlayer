using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CCWaterControllerPlayer.ViewModels;

namespace CCWaterControllerPlayer.Views;

public partial class TrackVisualizationControl : UserControl
{
    private static readonly Color MainTrackColor = Color.FromArgb(180, 108, 99, 255);
    private static readonly Color CurrentDotColor = Color.FromRgb(0, 255, 136);
    private static readonly Color CurrentDotGlowColor = Color.FromArgb(100, 0, 255, 136);
    private static readonly List<Color> CompareColors = new()
    {
        Color.FromRgb(255, 167, 38),
        Color.FromRgb(0, 217, 255),
        Color.FromRgb(255, 107, 107),
        Color.FromRgb(78, 203, 113),
        Color.FromRgb(233, 30, 99),
        Color.FromRgb(156, 39, 176),
        Color.FromRgb(0, 188, 212)
    };

    public static readonly DependencyProperty TrackPointsProperty =
        DependencyProperty.Register(nameof(TrackPoints), typeof(ObservableCollection<TrackPoint>),
            typeof(TrackVisualizationControl),
            new PropertyMetadata(null, OnTrackPointsChanged));

    public static readonly DependencyProperty HistoryTracksProperty =
        DependencyProperty.Register(nameof(HistoryTracks), typeof(ObservableCollection<List<TrackPoint>>),
            typeof(TrackVisualizationControl),
            new PropertyMetadata(null, OnHistoryTracksChanged));

    public static readonly DependencyProperty ShowGridProperty =
        DependencyProperty.Register(nameof(ShowGrid), typeof(bool),
            typeof(TrackVisualizationControl),
            new PropertyMetadata(true, OnVisualPropertyChanged));

    public static readonly DependencyProperty ShowCrosshairProperty =
        DependencyProperty.Register(nameof(ShowCrosshair), typeof(bool),
            typeof(TrackVisualizationControl),
            new PropertyMetadata(true, OnVisualPropertyChanged));

    public ObservableCollection<TrackPoint>? TrackPoints
    {
        get => (ObservableCollection<TrackPoint>?)GetValue(TrackPointsProperty);
        set => SetValue(TrackPointsProperty, value);
    }

    public ObservableCollection<List<TrackPoint>>? HistoryTracks
    {
        get => (ObservableCollection<List<TrackPoint>>?)GetValue(HistoryTracksProperty);
        set => SetValue(HistoryTracksProperty, value);
    }

    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public bool ShowCrosshair
    {
        get => (bool)GetValue(ShowCrosshairProperty);
        set => SetValue(ShowCrosshairProperty, value);
    }

    public TrackVisualizationControl()
    {
        InitializeComponent();
    }

    private static void OnTrackPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TrackVisualizationControl)d;
        if (e.OldValue is ObservableCollection<TrackPoint> oldCollection)
            oldCollection.CollectionChanged -= control.OnPointsCollectionChanged;
        if (e.NewValue is ObservableCollection<TrackPoint> newCollection)
            newCollection.CollectionChanged += control.OnPointsCollectionChanged;
        control.UpdateDataLayer();
    }

    private static void OnHistoryTracksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TrackVisualizationControl)d;
        if (e.OldValue is ObservableCollection<List<TrackPoint>> oldCol)
            oldCol.CollectionChanged -= control.OnHistoryCollectionChanged;
        if (e.NewValue is ObservableCollection<List<TrackPoint>> newCol)
            newCol.CollectionChanged += control.OnHistoryCollectionChanged;
        control.UpdateDataLayer();
    }

    private void OnHistoryCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateDataLayer();
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TrackVisualizationControl)d).Redraw();
    }

    private Polyline? _livePolyline;
    private Ellipse? _liveDot;
    private Ellipse? _liveDotGlow;
    private Polyline? _mainPolyline;
    private Ellipse? _mainDot;
    private readonly List<Polyline> _historyPolylines = new();
    private bool _backgroundDrawn;

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove && _livePolyline != null)
        {
            if (_livePolyline.Points.Count > 0)
                _livePolyline.Points.RemoveAt(0);
        }
        else if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems?.Count == 1)
        {
            AppendPointToLivePolyline((TrackPoint)e.NewItems[0]!);
        }
        else
        {
            UpdateDataLayer();
        }
    }

    private void TrackCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _backgroundDrawn = false;
        Redraw();
    }

    private (double size, double offsetX, double offsetY) GetSquareDrawArea()
    {
        double w = TrackCanvas.ActualWidth;
        double h = TrackCanvas.ActualHeight;
        double size = Math.Min(w, h);
        double offsetX = (w - size) / 2;
        double offsetY = (h - size) / 2;
        return (size, offsetX, offsetY);
    }

    private Point MapToCanvas(float stickX, float stickY, double size, double offsetX, double offsetY)
    {
        double px = offsetX + (stickX + 1) / 2 * size;
        double py = offsetY + (1 - (stickY + 1) / 2) * size;
        return new Point(px, py);
    }

    private void Redraw()
    {
        TrackCanvas.Children.Clear();
        _livePolyline = null;
        _liveDotGlow = null;
        _liveDot = null;
        _mainPolyline = null;
        _mainDot = null;
        _historyPolylines.Clear();
        _backgroundDrawn = false;

        double w = TrackCanvas.ActualWidth;
        double h = TrackCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var (size, offsetX, offsetY) = GetSquareDrawArea();

        if (ShowGrid) DrawGrid(size, offsetX, offsetY);
        if (ShowCrosshair) DrawCrosshair(size, offsetX, offsetY);
        _backgroundDrawn = true;

        UpdateDataLayer();
    }

    private void UpdateDataLayer()
    {
        double w = TrackCanvas.ActualWidth;
        double h = TrackCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        if (!_backgroundDrawn)
        {
            Redraw();
            return;
        }

        var (size, offsetX, offsetY) = GetSquareDrawArea();

        foreach (var p in _historyPolylines)
            TrackCanvas.Children.Remove(p);
        _historyPolylines.Clear();

        if (_mainPolyline != null) TrackCanvas.Children.Remove(_mainPolyline);
        if (_mainDot != null) TrackCanvas.Children.Remove(_mainDot);
        _mainPolyline = null;
        _mainDot = null;

        if (_livePolyline != null) TrackCanvas.Children.Remove(_livePolyline);
        if (_liveDotGlow != null) TrackCanvas.Children.Remove(_liveDotGlow);
        if (_liveDot != null) TrackCanvas.Children.Remove(_liveDot);
        _livePolyline = null;
        _liveDotGlow = null;
        _liveDot = null;

        if (HistoryTracks != null)
        {
            int colorIdx = 0;
            foreach (var track in HistoryTracks)
            {
                var polyline = CreatePolylineFromTrack(track, CompareColors[colorIdx % CompareColors.Count], size, offsetX, offsetY, 0.6);
                _historyPolylines.Add(polyline);
                TrackCanvas.Children.Add(polyline);
                colorIdx++;
            }
        }

        if (TrackPoints != null && TrackPoints.Count > 0)
        {
            _livePolyline = new Polyline
            {
                Stroke = new SolidColorBrush(MainTrackColor),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };
            foreach (var point in TrackPoints)
            {
                var pt = MapToCanvas(point.X, point.Y, size, offsetX, offsetY);
                _livePolyline.Points.Add(pt);
            }
            TrackCanvas.Children.Add(_livePolyline);

            var lastPt = MapToCanvas(TrackPoints[^1].X, TrackPoints[^1].Y, size, offsetX, offsetY);
            _liveDotGlow = new Ellipse { Width = 18, Height = 18, Fill = new SolidColorBrush(CurrentDotGlowColor) };
            Canvas.SetLeft(_liveDotGlow, lastPt.X - 9);
            Canvas.SetTop(_liveDotGlow, lastPt.Y - 9);
            TrackCanvas.Children.Add(_liveDotGlow);

            _liveDot = new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(CurrentDotColor) };
            Canvas.SetLeft(_liveDot, lastPt.X - 5);
            Canvas.SetTop(_liveDot, lastPt.Y - 5);
            TrackCanvas.Children.Add(_liveDot);
        }
    }

    private Polyline CreatePolylineFromTrack(List<TrackPoint> points, Color color, double size, double offsetX, double offsetY, double opacity)
    {
        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round,
            Opacity = opacity
        };

        foreach (var point in points)
        {
            var pt = MapToCanvas(point.X, point.Y, size, offsetX, offsetY);
            polyline.Points.Add(pt);
        }

        return polyline;
    }

    private void DrawGrid(double size, double offsetX, double offsetY)
    {
        var gridBrush = new SolidColorBrush(Color.FromArgb(40, 48, 54, 61));

        for (int i = 1; i < 4; i++)
        {
            double x = offsetX + size * i / 4;
            var line = new Line { X1 = x, Y1 = offsetY, X2 = x, Y2 = offsetY + size, Stroke = gridBrush, StrokeThickness = 1 };
            TrackCanvas.Children.Add(line);
        }
        for (int i = 1; i < 4; i++)
        {
            double y = offsetY + size * i / 4;
            var line = new Line { X1 = offsetX, Y1 = y, X2 = offsetX + size, Y2 = y, Stroke = gridBrush, StrokeThickness = 1 };
            TrackCanvas.Children.Add(line);
        }

        var circleBrush = new SolidColorBrush(Color.FromArgb(30, 108, 99, 255));
        var circle = new Ellipse
        {
            Width = size, Height = size,
            Stroke = circleBrush, StrokeThickness = 1
        };
        Canvas.SetLeft(circle, offsetX);
        Canvas.SetTop(circle, offsetY);
        TrackCanvas.Children.Add(circle);
    }

    private void DrawCrosshair(double size, double offsetX, double offsetY)
    {
        var brush = new SolidColorBrush(Color.FromArgb(80, 139, 148, 158));
        double cx = offsetX + size / 2;
        double cy = offsetY + size / 2;
        var hLine = new Line { X1 = offsetX, Y1 = cy, X2 = offsetX + size, Y2 = cy, Stroke = brush, StrokeThickness = 1 };
        var vLine = new Line { X1 = cx, Y1 = offsetY, X2 = cx, Y2 = offsetY + size, Stroke = brush, StrokeThickness = 1 };
        TrackCanvas.Children.Add(hLine);
        TrackCanvas.Children.Add(vLine);
    }



    private void AppendPointToLivePolyline(TrackPoint point)
    {
        double w = TrackCanvas.ActualWidth;
        double h = TrackCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var (size, offsetX, offsetY) = GetSquareDrawArea();
        var pt = MapToCanvas(point.X, point.Y, size, offsetX, offsetY);

        if (_livePolyline == null)
        {
            _livePolyline = new Polyline
            {
                Stroke = new SolidColorBrush(MainTrackColor),
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };
            TrackCanvas.Children.Add(_livePolyline);

            _liveDotGlow = new Ellipse
            {
                Width = 18, Height = 18,
                Fill = new SolidColorBrush(CurrentDotGlowColor)
            };
            TrackCanvas.Children.Add(_liveDotGlow);

            _liveDot = new Ellipse
            {
                Width = 10, Height = 10,
                Fill = new SolidColorBrush(CurrentDotColor)
            };
            TrackCanvas.Children.Add(_liveDot);
        }

        _livePolyline.Points.Add(pt);

        if (_liveDotGlow != null)
        {
            Canvas.SetLeft(_liveDotGlow, pt.X - 9);
            Canvas.SetTop(_liveDotGlow, pt.Y - 9);
        }

        if (_liveDot != null)
        {
            Canvas.SetLeft(_liveDot, pt.X - 5);
            Canvas.SetTop(_liveDot, pt.Y - 5);
        }
    }
}
