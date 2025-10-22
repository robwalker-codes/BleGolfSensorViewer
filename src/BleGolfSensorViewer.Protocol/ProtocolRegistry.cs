using System;
using System.Collections.Generic;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;
using BleGolfSensorViewer.Domain.Utility;
using BleGolfSensorViewer.Protocol.Parsers;

namespace BleGolfSensorViewer.Protocol;

/// <summary>
/// Provides protocol decoding by delegating to registered parsers.
/// </summary>
public class ProtocolRegistry : IProtocolDecoder
{
    private readonly IReadOnlyList<Func<Measurement, DecodedMeasurement?>> _parsers;

    public ProtocolRegistry()
    {
        _parsers = new List<Func<Measurement, DecodedMeasurement?>>
        {
            TryDecodeMotion
        };
    }

    public DecodedMeasurement Decode(Measurement measurement)
    {
        if (measurement is null)
        {
            throw new ArgumentNullException(nameof(measurement));
        }

        foreach (var parser in _parsers)
        {
            var decoded = parser(measurement);
            if (decoded is not null)
            {
                return decoded;
            }
        }

        return CreateUnknownMeasurement(measurement);
    }

    private static DecodedMeasurement? TryDecodeMotion(Measurement measurement)
    {
        return ExamplePhigolfParsers.TryParseMotionMeasurement(measurement, out var decoded) ? decoded : null;
    }

    private static DecodedMeasurement CreateUnknownMeasurement(Measurement measurement)
    {
        var fields = new Dictionary<string, string>
        {
            ["Info"] = "Unrecognised characteristic"
        };

        return new DecodedMeasurement(
            measurement.Timestamp,
            measurement.ServiceId,
            measurement.CharacteristicId,
            "Unknown",
            fields,
            HexEncoder.ToHex(measurement.RawBytes));
    }
}
