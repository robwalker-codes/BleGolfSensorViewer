using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace BleGolfSensorViewer.Infrastructure.Ble.WinRt;

/// <summary>
/// Handles GATT subscriptions using WinRT APIs.
/// </summary>
public sealed class GattSubscriptionServiceWinRt : IGattSubscriptionService
{
    private readonly Func<BluetoothLEDevice?> _deviceProvider;
    private readonly List<GattCharacteristic> _subscribedCharacteristics = new();
    private readonly Dictionary<GattCharacteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>> _handlers = new();

    public GattSubscriptionServiceWinRt(Func<BluetoothLEDevice?> deviceProvider)
    {
        _deviceProvider = deviceProvider ?? throw new ArgumentNullException(nameof(deviceProvider));
    }

    public event EventHandler<Measurement>? MeasurementReceived;

    public async Task SubscribeAsync(BleDeviceId deviceId, CancellationToken cancellationToken)
    {
        if (deviceId is null)
        {
            throw new ArgumentNullException(nameof(deviceId));
        }

        var device = _deviceProvider() ?? throw new InvalidOperationException("Device must be connected before subscribing.");
        var servicesResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken).ConfigureAwait(false);
        if (servicesResult.Status != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException($"Failed to enumerate services: {servicesResult.Status}");
        }

        foreach (var service in servicesResult.Services)
        {
            var characteristicsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached).AsTask(cancellationToken).ConfigureAwait(false);
            if (characteristicsResult.Status != GattCommunicationStatus.Success)
            {
                continue;
            }

            foreach (var characteristic in characteristicsResult.Characteristics)
            {
                if (!SupportsNotifications(characteristic))
                {
                    continue;
                }

                await EnableNotificationsAsync(characteristic, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task UnsubscribeAsync(CancellationToken cancellationToken)
    {
        foreach (var characteristic in _subscribedCharacteristics)
        {
            if (_handlers.TryGetValue(characteristic, out var handler))
            {
                characteristic.ValueChanged -= handler;
                _handlers.Remove(characteristic);
            }

            await characteristic
                .WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);
        }

        _subscribedCharacteristics.Clear();
        _handlers.Clear();
    }

    private static bool SupportsNotifications(GattCharacteristic characteristic)
    {
        var properties = characteristic.CharacteristicProperties;
        return properties.HasFlag(GattCharacteristicProperties.Notify) || properties.HasFlag(GattCharacteristicProperties.Indicate);
    }

    private async Task EnableNotificationsAsync(GattCharacteristic characteristic, CancellationToken cancellationToken)
    {
        var descriptor = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify)
            ? GattClientCharacteristicConfigurationDescriptorValue.Notify
            : GattClientCharacteristicConfigurationDescriptorValue.Indicate;

        var status = await characteristic
            .WriteClientCharacteristicConfigurationDescriptorAsync(descriptor)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);

        if (status != GattCommunicationStatus.Success)
        {
            return;
        }

        TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> handler = (sender, args) =>
        {
            var bytes = ReadBuffer(args.CharacteristicValue);
            var serviceId = sender.Service?.Uuid ?? Guid.Empty;
            var measurement = new Measurement(DateTimeOffset.UtcNow, serviceId, sender.Uuid, bytes);
            MeasurementReceived?.Invoke(this, measurement);
        };

        characteristic.ValueChanged += handler;
        _handlers[characteristic] = handler;
        _subscribedCharacteristics.Add(characteristic);
    }

    private static byte[] ReadBuffer(IBuffer buffer)
    {
        if (buffer is null)
        {
            return Array.Empty<byte>();
        }

        using var reader = DataReader.FromBuffer(buffer);
        var length = (int)buffer.Length;
        var data = new byte[length];
        reader.ReadBytes(data);
        return data;
    }

    public async ValueTask DisposeAsync()
    {
        await UnsubscribeAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
