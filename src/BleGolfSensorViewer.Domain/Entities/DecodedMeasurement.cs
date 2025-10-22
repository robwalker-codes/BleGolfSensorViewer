using System;
using System.Collections.Generic;

namespace BleGolfSensorViewer.Domain.Entities;

/// <summary>
/// Represents a decoded measurement with human-readable fields.
/// </summary>
public sealed class DecodedMeasurement
{
    public DecodedMeasurement(
        DateTimeOffset timestamp,
        Guid serviceId,
        Guid characteristicId,
        string name,
        IReadOnlyDictionary<string, string> fields,
        string rawHex)
    {
        Timestamp = timestamp;
        ServiceId = serviceId;
        CharacteristicId = characteristicId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        RawHex = rawHex ?? throw new ArgumentNullException(nameof(rawHex));
    }

    public DateTimeOffset Timestamp { get; }

    public Guid ServiceId { get; }

    public Guid CharacteristicId { get; }

    public string Name { get; }

    public IReadOnlyDictionary<string, string> Fields { get; }

    public string RawHex { get; }
}
