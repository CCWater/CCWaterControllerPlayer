using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using CCWaterControllerPlayer.Models;

namespace CCWaterControllerPlayer.Overlay;

public partial class ImageOverlayWindow : Window
{
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int GWL_EXSTYLE = -20;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private bool _isClickThrough;
    private bool _isPinned;

    public bool IsPinned => _isPinned;
    public Action? OnPositionChanged { get; set; }

    public ImageOverlayWindow()
    {
        InitializeComponent();
        LocationChanged += (_, _) => NotifyPositionChanged();
        SizeChanged += (_, _) => NotifyPositionChanged();
    }

    private void NotifyPositionChanged()
    {
        if (!_isPinned && IsLoaded)
            OnPositionChanged?.Invoke();
    }

    public void Configure(ImageOverlayConfig config)
    {
        Width = config.Width;
        Height = config.Height;
        Left = config.PositionX;
        Top = config.PositionY;
        Opacity = config.Opacity;

        if (!string.IsNullOrEmpty(config.ImagePath) && File.Exists(config.ImagePath))
        {
            LoadImage(config.ImagePath);
        }
    }

    public void LoadImage(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            OverlayImage.Source = bitmap;
            PlaceholderText.Visibility = Visibility.Collapsed;

            var fileName = Path.GetFileName(path);
            TitleText.Text = fileName.Length > 20 ? fileName[..17] + "..." : fileName;
        }
        catch
        {
            OverlayImage.Source = null;
            PlaceholderText.Visibility = Visibility.Visible;
        }
    }

    public void Pin()
    {
        _isPinned = true;
        TitleBar.Visibility = Visibility.Collapsed;
        SetClickThrough(true);
    }

    public void Unpin()
    {
        _isPinned = false;
        SetClickThrough(false);
        TitleBar.Visibility = Visibility.Visible;
    }

    private void SetClickThrough(bool enabled)
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

    public void SavePosition(ImageOverlayConfig config)
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

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0 && IsImageFile(files[0]))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
                return;
            }
        }
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void OnImageDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files.Length == 0) return;

        var file = files[0];
        if (IsImageFile(file))
        {
            LoadImage(file);
            OnImageChanged?.Invoke(file);
        }
    }

    private void OnPlaceholderClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isPinned) return;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.tiff|All Files|*.*"
        };
        if (dialog.ShowDialog() == true && IsImageFile(dialog.FileName))
        {
            LoadImage(dialog.FileName);
            OnImageChanged?.Invoke(dialog.FileName);
        }
    }

    public Action<string>? OnImageChanged { get; set; }

    private static bool IsImageFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" or ".tiff";
    }
}
