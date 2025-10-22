#if !WINDOWS
using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Infrastructure.Ble.WinRt;

/// <summary>
/// Non-Windows placeholder implementation so multi-targeted builds succeed.
/// </summary>
public sealed class BleDeviceConnectorWinRt : IBleDeviceConnector
{
    public event EventHandler<bool>? ConnectionStateChanged;

    public bool IsConnected => false;

    public Task<BleConnectionDiagnostics> ConnectAsync(BleDeviceId deviceId, CancellationToken cancellationToken)
    {
        throw new PlatformNotSupportedException("WinRT BLE pairing is only supported when running on Windows.");
    }

    public Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
#endif
