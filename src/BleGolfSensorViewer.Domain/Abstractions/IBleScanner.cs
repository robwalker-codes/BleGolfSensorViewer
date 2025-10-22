using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Domain.Abstractions;

/// <summary>
/// Scans for BLE devices that expose golf sensor characteristics.
/// </summary>
public interface IBleScanner : IAsyncDisposable
{
    event EventHandler<DiscoveredDevice>? DeviceDiscovered;

    Task StartScanningAsync(CancellationToken cancellationToken);

    Task StopScanningAsync(CancellationToken cancellationToken);
}
