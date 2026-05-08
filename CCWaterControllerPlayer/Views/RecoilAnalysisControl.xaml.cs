using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CCWaterControllerPlayer.Views;

public record RecoilPoint(float X, float Y, double TimestampMs);

public partial class RecoilAnalysisControl : UserControl
{
    private static readonly Color PreFireInputColor = Color.FromArgb(200, 100, 180, 255);
    private static readonly Color FiringInputColor = Color.FromArgb(220, 108, 99, 255);
    private static readonly Color PostFireInputColor = Color.FromArgb(160, 160, 140, 255);

    private static readonly Color PreFireRecoilColor = Color.FromArgb(200, 255, 200, 80);
    private static readonly Color FiringRecoilColor = Color.FromArgb(220, 255, 167, 38);
    private static readonly Color PostFireRecoilColor = Color.FromArgb(160, 200, 140, 100);

    private static readonly Color InputDotColor = Color.FromRgb(0, 255, 136);
    private static readonly Color InputDotGlowColor = Color.FromArgb(100, 0, 255, 136);
    private static readonly Color RecoilDotColor = Color.FromRgb(255, 100, 100);
    private static readonly Color RecoilDotGlowColor = Color.FromArgb(100, 255, 100, 100);
    private static readonly Color GridColor = Color.FromArgb(40, 48, 54, 61);
    private static readonly Color CrosshairColor = Color.FromArgb(80, 139, 148, 158);
    private static readonly Color OriginColor = Color.FromArgb(120, 255, 255, 255);

    public static readonly DependencyProperty InputTrackProperty =
        DependencyProperty.Register(nameof(InputTrack), typeof(ObservableCollection<RecoilPoint>),
            typeof(RecoilAnalysisControl),
            new PropertyMetadata(null, OnTrackChanged));

    public static readonly DependencyProperty RecoilTrackProperty =
        DependencyProperty.Register(nameof(RecoilTrack), typeof(ObservableCollection<RecoilPoint>),
            typeof(RecoilAnalysisControl),
            new PropertyMetadata(null, OnTrackChanged));

    public static readonly DependencyProperty CurrentIndexProperty =
        DependencyProperty.Register(nameof(CurrentIndex), typeof(int),
            typeof(RecoilAnalysisControl),
            new PropertyMetadata(-1, OnCurrentIndexChanged));

    public static readonly DependencyProperty TriggerStartMsProperty =
        DependencyProperty.Register(nameof(TriggerStartMs), typeof(double),
            typeof(RecoilAnalysisControl),
            new PropertyMetadata(0.0, OnPhaseChanged));

    public static readonly DependencyProperty TriggerEndMsProperty =
        DependencyProperty.Register(nameof(TriggerEndMs), typeof(double),
            typeof(RecoilAnalysisControl),
            new PropertyMetadata(0.0, OnPhaseChanged));

    public static readonly DependencyProperty ShowInputTrackProperty =
        DependencyProperty.Register(nameof(ShowInputTrack), typeof(bool),
            typeof(RecoilAnalysisControl),
            new PropertyMetadata(true, OnVisibilityChanged));

    public static readonly DependencyProperty ShowRecoilTrackProperty =
        DependencyProperty.Register(nameof(ShowRecoilTrack), typeof(bool),
            typeof(RecoilAnalysisControl),
            new PropertyMetadata(true, OnVisibilityChanged));

    public ObservableCollection<RecoilPoint>? InputTrack
    {
        get => (ObservableCollection<RecoilPoint>?)GetValue(InputTrackProperty);
        set => SetValue(InputTrackProperty, value);
    }

    public ObservableCollection<RecoilPoint>? RecoilTrack
    {
        get => (ObservableCollection<RecoilPoint>?)GetValue(RecoilTrackProperty);
        set => SetValue(RecoilTrackProperty, value);
    }

    public int CurrentIndex
    {
        get => (int)GetValue(CurrentIndexProperty);
        set => SetValue(CurrentIndexProperty, value);
    }

    public double TriggerStartMs
    {
        get => (double)GetValue(TriggerStartMsProperty);
        set => SetValue(TriggerStartMsProperty, value);
    }

    public double TriggerEndMs
    {
        get => (double)GetValue(TriggerEndMsProperty);
        set => SetValue(TriggerEndMsProperty, value);
    }

    public bool ShowInputTrack
    {
        get => (bool)GetValue(ShowInputTrackProperty);
        set => SetValue(ShowInputTrackProperty, value);
    }

    public bool ShowRecoilTrack
    {
        get => (bool)GetValue(ShowRecoilTrackProperty);
        set => SetValue(ShowRecoilTrackProperty, value);
    }

    public RecoilAnalysisControl()
    {
        InitializeComponent();
    }

    private static void OnTrackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (RecoilAnalysisControl)d;
        if (e.OldValue is ObservableCollection<RecoilPoint> oldCol)
            oldCol.CollectionChanged -= control.OnTrackCollectionChanged;
        if (e.NewValue is ObservableCollection<RecoilPoint> newCol)
            newCol.CollectionChanged += control.OnTrackCollectionChanged;
        control.FullRedraw();
    }

    private static void OnCurrentIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (RecoilAnalysisControl)d;
        control.UpdateCurrentDots();
    }

    private static void OnPhaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (RecoilAnalysisControl)d;
        control.FullRedraw();
    }

    private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (RecoilAnalysisControl)d;
        control.FullRedraw();
    }

    private void OnTrackCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        FullRedraw();
    }

    private void AnalysisCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        FullRedraw();
    }

    private Ellipse? _inputDot;
    private Ellipse? _inputDotGlow;
    private Ellipse? _recoilDot;
    private Ellipse? _recoilDotGlow;
    private Ellipse? _originMarker;

    private double _scale = 1;
    private double _centerX;
    private double _centerY;

    private (double minX, double maxX, double minY, double maxY) ComputeBounds()
    {
        double minX = 0, maxX = 0, minY = 0, maxY = 0;

        if (ShowInputTrack && InputTrack != null)
        {
            foreach (var p in InputTrack)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
        }

        if (ShowRecoilTrack && RecoilTrack != null)
        {
            foreach (var p in RecoilTrack)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
        }

        double margin = 0.1;
        double rangeX = maxX - minX;
        double rangeY = maxY - minY;
        if (rangeX < 0.01) rangeX = 1;
        if (rangeY < 0.01) rangeY = 1;

        return (minX - rangeX * margin, maxX + rangeX * margin,
                minY - rangeY * margin, maxY + rangeY * margin);
    }

    private Point MapToCanvas(float dataX, float dataY)
    {
        double px = _centerX + dataX * _scale;
        double py = _centerY - dataY * _scale;
        return new Point(px, py);
    }

    private void ComputeTransform()
    {
        double w = AnalysisCanvas.ActualWidth;
        double h = AnalysisCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var (minX, maxX, minY, maxY) = ComputeBounds();
        double rangeX = maxX - minX;
        double rangeY = maxY - minY;

        double scaleX = w / rangeX;
        double scaleY = h / rangeY;
        _scale = Math.Min(scaleX, scaleY);

        double dataCenterX = (minX + maxX) / 2;
        double dataCenterY = (minY + maxY) / 2;
        _centerX = w / 2 - dataCenterX * _scale;
        _centerY = h / 2 + dataCenterY * _scale;
    }

    private void FullRedraw()
    {
        AnalysisCanvas.Children.Clear();
        _inputDot = null;
        _inputDotGlow = null;
        _recoilDot = null;
        _recoilDotGlow = null;
        _originMarker = null;

        double w = AnalysisCanvas.ActualWidth;
        double h = AnalysisCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        ComputeTransform();
        DrawBackground();
        DrawTracks();
        UpdateCurrentDots();
    }

    private void DrawBackground()
    {
        var origin = MapToCanvas(0, 0);

        var hLine = new Line
        {
            X1 = 0, Y1 = origin.Y,
            X2 = AnalysisCanvas.ActualWidth, Y2 = origin.Y,
            Stroke = new SolidColorBrush(CrosshairColor), StrokeThickness = 1
        };
        AnalysisCanvas.Children.Add(hLine);

        var vLine = new Line
        {
            X1 = origin.X, Y1 = 0,
            X2 = origin.X, Y2 = AnalysisCanvas.ActualHeight,
            Stroke = new SolidColorBrush(CrosshairColor), StrokeThickness = 1
        };
        AnalysisCanvas.Children.Add(vLine);

        _originMarker = new Ellipse
        {
            Width = 8, Height = 8,
            Fill = new SolidColorBrush(OriginColor)
        };
        Canvas.SetLeft(_originMarker, origin.X - 4);
        Canvas.SetTop(_originMarker, origin.Y - 4);
        AnalysisCanvas.Children.Add(_originMarker);
    }

    private void DrawTracks()
    {
        if (ShowInputTrack && InputTrack != null && InputTrack.Count > 0)
        {
            DrawSegmentedTrack(InputTrack, PreFireInputColor, FiringInputColor, PostFireInputColor, false);
        }

        if (ShowRecoilTrack && RecoilTrack != null && RecoilTrack.Count > 0)
        {
            DrawSegmentedTrack(RecoilTrack, PreFireRecoilColor, FiringRecoilColor, PostFireRecoilColor, true);
        }
    }

    private void DrawSegmentedTrack(ObservableCollection<RecoilPoint> track, Color preColor, Color fireColor, Color postColor, bool dashed)
    {
        double triggerStart = TriggerStartMs;
        double triggerEnd = TriggerEndMs;

        var prePoints = new PointCollection();
        var firePoints = new PointCollection();
        var postPoints = new PointCollection();

        Point? lastPrePoint = null;
        Point? lastFirePoint = null;

        foreach (var p in track)
        {
            var pt = MapToCanvas(p.X, p.Y);
            if (p.TimestampMs < triggerStart)
            {
                prePoints.Add(pt);
                lastPrePoint = pt;
            }
            else if (p.TimestampMs <= triggerEnd)
            {
                if (firePoints.Count == 0 && lastPrePoint.HasValue)
                    firePoints.Add(lastPrePoint.Value);
                firePoints.Add(pt);
                lastFirePoint = pt;
            }
            else
            {
                if (postPoints.Count == 0 && lastFirePoint.HasValue)
                    postPoints.Add(lastFirePoint.Value);
                else if (postPoints.Count == 0 && lastPrePoint.HasValue && firePoints.Count == 0)
                    postPoints.Add(lastPrePoint.Value);
                postPoints.Add(pt);
            }
        }

        if (prePoints.Count > 1)
            AddPolyline(prePoints, preColor, dashed);
        if (firePoints.Count > 1)
            AddPolyline(firePoints, fireColor, dashed);
        if (postPoints.Count > 1)
            AddPolyline(postPoints, postColor, dashed);
    }

    private void AddPolyline(PointCollection points, Color color, bool dashed)
    {
        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round,
            Points = points
        };
        if (dashed)
            polyline.StrokeDashArray = new DoubleCollection { 4, 2 };
        AnalysisCanvas.Children.Add(polyline);
    }

    private void UpdateCurrentDots()
    {
        if (_inputDotGlow != null) AnalysisCanvas.Children.Remove(_inputDotGlow);
        if (_inputDot != null) AnalysisCanvas.Children.Remove(_inputDot);
        if (_recoilDotGlow != null) AnalysisCanvas.Children.Remove(_recoilDotGlow);
        if (_recoilDot != null) AnalysisCanvas.Children.Remove(_recoilDot);
        _inputDotGlow = null;
        _inputDot = null;
        _recoilDotGlow = null;
        _recoilDot = null;

        int idx = CurrentIndex;
        if (idx < 0) return;

        if (ShowInputTrack && InputTrack != null && idx < InputTrack.Count)
        {
            var p = InputTrack[idx];
            var pt = MapToCanvas(p.X, p.Y);

            _inputDotGlow = new Ellipse { Width = 18, Height = 18, Fill = new SolidColorBrush(InputDotGlowColor) };
            Canvas.SetLeft(_inputDotGlow, pt.X - 9);
            Canvas.SetTop(_inputDotGlow, pt.Y - 9);
            AnalysisCanvas.Children.Add(_inputDotGlow);

            _inputDot = new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(InputDotColor) };
            Canvas.SetLeft(_inputDot, pt.X - 5);
            Canvas.SetTop(_inputDot, pt.Y - 5);
            AnalysisCanvas.Children.Add(_inputDot);
        }

        if (ShowRecoilTrack && RecoilTrack != null && idx < RecoilTrack.Count)
        {
            var p = RecoilTrack[idx];
            var pt = MapToCanvas(p.X, p.Y);

            _recoilDotGlow = new Ellipse { Width = 18, Height = 18, Fill = new SolidColorBrush(RecoilDotGlowColor) };
            Canvas.SetLeft(_recoilDotGlow, pt.X - 9);
            Canvas.SetTop(_recoilDotGlow, pt.Y - 9);
            AnalysisCanvas.Children.Add(_recoilDotGlow);

            _recoilDot = new Ellipse { Width = 10, Height = 10, Fill = new SolidColorBrush(RecoilDotColor) };
            Canvas.SetLeft(_recoilDot, pt.X - 5);
            Canvas.SetTop(_recoilDot, pt.Y - 5);
            AnalysisCanvas.Children.Add(_recoilDot);
        }
    }
}
