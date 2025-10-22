using System;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Domain.Abstractions;

/// <summary>
/// Decodes measurement payloads into rich domain data.
/// </summary>
public interface IProtocolDecoder
{
    DecodedMeasurement Decode(Measurement measurement);
}
