using System;
using System.Collections.Generic;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Infrastructure.Ble.Protocol.Parsers;

/// <summary>
/// Demonstrates decoding PhiGolf-like payloads.
/// </summary>
public static class ExamplePhigolfParsers
{
    public static bool TryParseMotionMeasurement(Measurement measurement, out DecodedMeasurement? decoded)
    {
        if (measurement.ServiceId != Protocol.KnownUuids.MotionService || measurement.CharacteristicId != Protocol.KnownUuids.MotionDataCharacteristic)
        {
            decoded = null;
            return false;
        }

        if (measurement.RawBytes.Length < sizeof(float) * 4)
        {
            decoded = null;
            return false;
        }

        try
        {
            var buffer = measurement.RawBytes.AsSpan();
            var speed = FloatParser.ReadLittleEndianFloat(buffer, 0);
            var faceAngle = FloatParser.ReadLittleEndianFloat(buffer, 4);
            var path = FloatParser.ReadLittleEndianFloat(buffer, 8);
            var tempo = FloatParser.ReadLittleEndianFloat(buffer, 12);

            var fields = new Dictionary<string, string>
            {
                ["SpeedMps"] = speed.ToString("F2"),
                ["FaceAngleDeg"] = faceAngle.ToString("F2"),
                ["PathDeg"] = path.ToString("F2"),
                ["Tempo"] = tempo.ToString("F2")
            };

            decoded = new DecodedMeasurement(
                measurement.Timestamp,
                measurement.ServiceId,
                measurement.CharacteristicId,
                "Motion",
                fields,
                Domain.Utility.HexEncoder.ToHex(measurement.RawBytes));
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            decoded = null;
            return false;
        }
    }
}
