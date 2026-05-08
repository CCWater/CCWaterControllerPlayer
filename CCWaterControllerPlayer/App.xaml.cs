using System.IO;
using System.Windows;
using System.Windows.Threading;
using CCWaterControllerPlayer.Models;
using CCWaterControllerPlayer.Services;
using CCWaterControllerPlayer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CCWaterControllerPlayer;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogAndShowError(e.Exception);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogAndShowError(ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogAndShowError(e.Exception);
        e.SetObserved();
    }

    private static void LogAndShowError(Exception ex)
    {
        var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n\n";
        try
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
            File.AppendAllText(logPath, msg);
        }
        catch { }
        MessageBox.Show(ex.Message, "CCWater Controller Player - Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SettingsService>();
        services.AddSingleton<DatabaseService>();
        services.AddSingleton(sp =>
        {
            var settingsService = sp.GetRequiredService<SettingsService>();
            return new RecordingService(settingsService);
        });
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
