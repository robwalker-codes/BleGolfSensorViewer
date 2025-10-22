using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;
using Windows.Devices.Bluetooth.Advertisement;

namespace BleGolfSensorViewer.Infrastructure.Ble.WinRt;

/// <summary>
/// WinRT implementation of <see cref="IBleScanner"/> using <see cref="BluetoothLEAdvertisementWatcher"/>.
/// </summary>
public sealed class BleScannerWinRt : IBleScanner
{
    private readonly BluetoothLEAdvertisementWatcher _watcher;

    public BleScannerWinRt()
    {
        _watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };
        _watcher.Received += OnAdvertisementReceived;
    }

    public event EventHandler<DiscoveredDevice>? DeviceDiscovered;

    public Task StartScanningAsync(CancellationToken cancellationToken)
    {
        _watcher.Start();
        return Task.CompletedTask;
    }

    public Task StopScanningAsync(CancellationToken cancellationToken)
    {
        _watcher.Stop();
        return Task.CompletedTask;
    }

    private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
    {
        var name = string.IsNullOrWhiteSpace(args.Advertisement.LocalName) ? "Unknown" : args.Advertisement.LocalName;
        var device = new DiscoveredDevice(new BleDeviceId(args.BluetoothAddress), name, (short)args.RawSignalStrengthInDBm);
        DeviceDiscovered?.Invoke(this, device);
    }

    public ValueTask DisposeAsync()
    {
        _watcher.Received -= OnAdvertisementReceived;
        _watcher.Stop();
        _watcher.Dispose();
        return ValueTask.CompletedTask;
    }
}
