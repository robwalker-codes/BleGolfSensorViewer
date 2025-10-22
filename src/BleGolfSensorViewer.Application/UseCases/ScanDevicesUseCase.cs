using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Application.Contracts;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Application.UseCases;

/// <summary>
/// Coordinates BLE scanning operations.
/// </summary>
public sealed class ScanDevicesUseCase
{
    private readonly IBleScanner _scanner;
    private EventHandler<DiscoveredDevice>? _handler;
    private bool _isScanning;

    public ScanDevicesUseCase(IBleScanner scanner)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    public bool IsScanning => _isScanning;

    public async Task StartAsync(ScanDevicesRequest request, Action<DiscoveredDevice> deviceDiscovered, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (deviceDiscovered is null)
        {
            throw new ArgumentNullException(nameof(deviceDiscovered));
        }

        if (_isScanning)
        {
            return;
        }

        _handler = (_, device) => deviceDiscovered(device);
        _scanner.DeviceDiscovered += _handler;

        try
        {
            await _scanner.StartScanningAsync(cancellationToken).ConfigureAwait(false);
            _isScanning = true;
        }
        catch
        {
            _scanner.DeviceDiscovered -= _handler;
            _handler = null;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_isScanning)
        {
            return;
        }

        await _scanner.StopScanningAsync(cancellationToken).ConfigureAwait(false);
        if (_handler is not null)
        {
            _scanner.DeviceDiscovered -= _handler;
            _handler = null;
        }

        _isScanning = false;
    }
}
