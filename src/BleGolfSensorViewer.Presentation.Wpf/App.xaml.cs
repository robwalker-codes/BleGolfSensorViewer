using System;
using System.Threading.Tasks;
using System.Windows;
using BleGolfSensorViewer.Presentation.Wpf.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace BleGolfSensorViewer.Presentation.Wpf;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _serviceProvider = Bootstrapper.BuildServiceProvider();
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }

        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
