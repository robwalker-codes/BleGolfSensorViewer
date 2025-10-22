using BleGolfSensorViewer.Application.UseCases;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Infrastructure.Ble.Protocol;
using BleGolfSensorViewer.Infrastructure.Ble.WinRt;
using BleGolfSensorViewer.Presentation.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BleGolfSensorViewer.Presentation.Wpf.Composition;

/// <summary>
/// Configures dependency injection for the application.
/// </summary>
public static class Bootstrapper
{
    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<BleScannerWinRt>();
        services.AddSingleton<IBleScanner>(sp => sp.GetRequiredService<BleScannerWinRt>());

        services.AddSingleton<BleDeviceConnectorWinRt>();
        services.AddSingleton<IBleDeviceConnector>(sp => sp.GetRequiredService<BleDeviceConnectorWinRt>());

        services.AddSingleton<IGattSubscriptionService>(sp =>
        {
            var connector = sp.GetRequiredService<BleDeviceConnectorWinRt>();
            return new GattSubscriptionServiceWinRt(() => connector.ConnectedDevice);
        });

        services.AddSingleton<IProtocolDecoder, ProtocolRegistry>();

        services.AddSingleton<ScanDevicesUseCase>();
        services.AddSingleton<SubscribeNotificationsUseCase>();
        services.AddSingleton<ConnectDeviceUseCase>();
        services.AddSingleton<DisconnectDeviceUseCase>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
