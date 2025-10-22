using System;

namespace BleGolfSensorViewer.Domain.Entities;

/// <summary>
/// Represents a raw measurement notification from a BLE characteristic.
/// </summary>
public sealed class Measurement
{
    public Measurement(DateTimeOffset timestamp, Guid serviceId, Guid characteristicId, byte[] rawBytes)
    {
        if (rawBytes is null)
        {
            throw new ArgumentNullException(nameof(rawBytes));
        }

        Timestamp = timestamp;
        ServiceId = serviceId;
        CharacteristicId = characteristicId;
        RawBytes = rawBytes;
    }

    public DateTimeOffset Timestamp { get; }

    public Guid ServiceId { get; }

    public Guid CharacteristicId { get; }

    public byte[] RawBytes { get; }
}
