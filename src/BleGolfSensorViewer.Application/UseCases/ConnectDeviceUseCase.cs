using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Application.Contracts;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Errors;

namespace BleGolfSensorViewer.Application.UseCases;

/// <summary>
/// Manages connection workflow to a selected device.
/// </summary>
public sealed class ConnectDeviceUseCase
{
    private readonly IBleDeviceConnector _connector;

    public ConnectDeviceUseCase(IBleDeviceConnector connector)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
    }

    public async Task ExecuteAsync(ConnectDeviceRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.Device is null)
        {
            throw new ArgumentException("A device must be supplied to connect.", nameof(request));
        }

        if (_connector.IsConnected)
        {
            throw BleErrors.AlreadyConnected();
        }

        await _connector.ConnectAsync(request.Device.Id, cancellationToken).ConfigureAwait(false);
    }
}
