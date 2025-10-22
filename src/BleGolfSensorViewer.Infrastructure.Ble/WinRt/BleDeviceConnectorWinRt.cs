using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;
using Windows.Devices.Bluetooth;

namespace BleGolfSensorViewer.Infrastructure.Ble.WinRt;

/// <summary>
/// WinRT-based device connector.
/// </summary>
public sealed class BleDeviceConnectorWinRt : IBleDeviceConnector
{
    private BluetoothLEDevice? _device;

    public event EventHandler<bool>? ConnectionStateChanged;

    public BluetoothLEDevice? ConnectedDevice => _device;

    public bool IsConnected => _device?.ConnectionStatus == BluetoothConnectionStatus.Connected;

    public async Task ConnectAsync(BleDeviceId deviceId, CancellationToken cancellationToken)
    {
        if (deviceId is null)
        {
            throw new ArgumentNullException(nameof(deviceId));
        }

        if (_device is not null)
        {
            await DisposeCurrentDeviceAsync().ConfigureAwait(false);
        }

        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceId.Address).AsTask(cancellationToken).ConfigureAwait(false);
        if (device is null)
        {
            throw new InvalidOperationException("Bluetooth device could not be created.");
        }

        _device = device;
        _device.ConnectionStatusChanged += OnConnectionStatusChanged;
        RaiseConnectionStateChanged(IsConnected);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_device is null)
        {
            return;
        }

        await DisposeCurrentDeviceAsync().ConfigureAwait(false);
        RaiseConnectionStateChanged(false);
    }

    private async Task DisposeCurrentDeviceAsync()
    {
        if (_device is null)
        {
            return;
        }

        _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _device.Dispose();
        await Task.Yield();
        _device = null;
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        RaiseConnectionStateChanged(IsConnected);
    }

    private void RaiseConnectionStateChanged(bool isConnected)
    {
        ConnectionStateChanged?.Invoke(this, isConnected);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCurrentDeviceAsync().ConfigureAwait(false);
    }
}
