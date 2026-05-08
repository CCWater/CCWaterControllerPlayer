using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CCWaterControllerPlayer.ViewModels;

namespace CCWaterControllerPlayer;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnableDarkTitleBar();
        await _viewModel.InitializeAsync();
        _viewModel.RestoreMainWindowState(this);
    }

    private void EnableDarkTitleBar()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        int value = 1;
        DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.SaveMainWindowState(this);
        await _viewModel.ShutdownAsync();
    }
}
