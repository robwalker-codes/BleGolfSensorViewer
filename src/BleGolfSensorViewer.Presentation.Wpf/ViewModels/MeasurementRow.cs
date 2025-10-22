using System;
using System.Text;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Presentation.Wpf.ViewModels;

/// <summary>
/// Represents a row in the measurement grid.
/// </summary>
public sealed class MeasurementRow
{
    private MeasurementRow(DateTimeOffset timestamp, Guid serviceId, Guid characteristicId, string name, string parsedSummary, string hex)
    {
        Timestamp = timestamp;
        ServiceId = serviceId;
        CharacteristicId = characteristicId;
        Name = name;
        ParsedSummary = parsedSummary;
        Hex = hex;
    }

    public DateTimeOffset Timestamp { get; }

    public Guid ServiceId { get; }

    public Guid CharacteristicId { get; }

    public string Name { get; }

    public string ParsedSummary { get; }

    public string Hex { get; }

    public static MeasurementRow FromDecodedMeasurement(DecodedMeasurement decoded)
    {
        if (decoded is null)
        {
            throw new ArgumentNullException(nameof(decoded));
        }

        var parsedSummary = FormatFields(decoded);
        return new MeasurementRow(decoded.Timestamp, decoded.ServiceId, decoded.CharacteristicId, decoded.Name, parsedSummary, decoded.RawHex);
    }

    private static string FormatFields(DecodedMeasurement decoded)
    {
        if (decoded.Fields.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var pair in decoded.Fields)
        {
            builder.Append(pair.Key).Append('=').Append(pair.Value).Append(' ');
        }

        return builder.ToString().TrimEnd();
    }
}
