using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Domain.Abstractions;

/// <summary>
/// Handles connections to BLE devices.
/// </summary>
public interface IBleDeviceConnector : IAsyncDisposable
{
    event EventHandler<bool>? ConnectionStateChanged;

    Task ConnectAsync(BleDeviceId deviceId, CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);

    bool IsConnected { get; }
}
