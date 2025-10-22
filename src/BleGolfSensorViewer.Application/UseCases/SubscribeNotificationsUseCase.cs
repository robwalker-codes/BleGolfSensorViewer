using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Application.UseCases;

/// <summary>
/// Coordinates GATT notification subscriptions.
/// </summary>
public sealed class SubscribeNotificationsUseCase
{
    private readonly IGattSubscriptionService _subscriptionService;
    private EventHandler<Measurement>? _handler;
    private bool _isSubscribed;

    public SubscribeNotificationsUseCase(IGattSubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
    }

    public bool IsSubscribed => _isSubscribed;

    public async Task StartAsync(BleDeviceId deviceId, EventHandler<Measurement> measurementReceived, CancellationToken cancellationToken)
    {
        if (deviceId is null)
        {
            throw new ArgumentNullException(nameof(deviceId));
        }

        if (measurementReceived is null)
        {
            throw new ArgumentNullException(nameof(measurementReceived));
        }

        if (_isSubscribed)
        {
            return;
        }

        _handler = measurementReceived;
        _subscriptionService.MeasurementReceived += _handler;

        try
        {
            await _subscriptionService.SubscribeAsync(deviceId, cancellationToken).ConfigureAwait(false);
            _isSubscribed = true;
        }
        catch
        {
            _subscriptionService.MeasurementReceived -= _handler;
            _handler = null;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_isSubscribed)
        {
            return;
        }

        await _subscriptionService.UnsubscribeAsync(cancellationToken).ConfigureAwait(false);
        if (_handler is not null)
        {
            _subscriptionService.MeasurementReceived -= _handler;
            _handler = null;
        }

        _isSubscribed = false;
    }
}
