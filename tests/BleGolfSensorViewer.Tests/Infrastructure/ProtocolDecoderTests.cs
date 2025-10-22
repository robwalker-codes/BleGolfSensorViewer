using System;
using BleGolfSensorViewer.Domain.Entities;
using BleGolfSensorViewer.Infrastructure.Ble.Protocol;
using Xunit;

namespace BleGolfSensorViewer.Tests.Infrastructure;

public class ProtocolDecoderTests
{
    [Fact]
    public void Decode_KnownMotionMeasurement_ReturnsParsedFields()
    {
        var registry = new ProtocolRegistry();
        var buffer = new byte[16];
        BitConverter.GetBytes(45.5f).CopyTo(buffer, 0);
        BitConverter.GetBytes(-2.3f).CopyTo(buffer, 4);
        BitConverter.GetBytes(1.1f).CopyTo(buffer, 8);
        BitConverter.GetBytes(3.2f).CopyTo(buffer, 12);
        var measurement = new Measurement(DateTimeOffset.UtcNow, KnownUuids.MotionService, KnownUuids.MotionDataCharacteristic, buffer);

        var decoded = registry.Decode(measurement);

        Assert.Equal("Motion", decoded.Name);
        Assert.Equal("45.50", decoded.Fields["SpeedMps"]);
        Assert.Equal(BitConverter.ToString(buffer).Replace("-", string.Empty), decoded.RawHex);
    }

    [Fact]
    public void Decode_UnknownMeasurement_ReturnsUnknown()
    {
        var registry = new ProtocolRegistry();
        var measurement = new Measurement(DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), new byte[] { 0x01 });

        var decoded = registry.Decode(measurement);

        Assert.Equal("Unknown", decoded.Name);
        Assert.True(decoded.Fields.ContainsKey("Info"));
    }
}
