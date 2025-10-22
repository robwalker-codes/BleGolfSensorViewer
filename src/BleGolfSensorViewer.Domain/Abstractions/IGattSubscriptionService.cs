using System;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Domain.Abstractions;

/// <summary>
/// Manages subscriptions to GATT characteristics for measurement streaming.
/// </summary>
public interface IGattSubscriptionService : IAsyncDisposable
{
    event EventHandler<Measurement>? MeasurementReceived;

    Task SubscribeAsync(BleDeviceId deviceId, CancellationToken cancellationToken);

    Task UnsubscribeAsync(CancellationToken cancellationToken);
}
