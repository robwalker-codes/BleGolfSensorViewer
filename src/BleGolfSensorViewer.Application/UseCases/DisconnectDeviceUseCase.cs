using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Errors;

namespace BleGolfSensorViewer.Application.UseCases;

/// <summary>
/// Handles disconnection from BLE devices.
/// </summary>
public sealed class DisconnectDeviceUseCase
{
    private readonly IBleDeviceConnector _connector;
    private readonly SubscribeNotificationsUseCase _subscriptionUseCase;

    public DisconnectDeviceUseCase(IBleDeviceConnector connector, SubscribeNotificationsUseCase subscriptionUseCase)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
        _subscriptionUseCase = subscriptionUseCase ?? throw new ArgumentNullException(nameof(subscriptionUseCase));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_connector.IsConnected)
        {
            throw BleErrors.NotConnected();
        }

        await _subscriptionUseCase.StopAsync(cancellationToken).ConfigureAwait(false);
        await _connector.DisconnectAsync(cancellationToken).ConfigureAwait(false);
    }
}
