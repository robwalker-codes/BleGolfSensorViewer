using BleGolfSensorViewer.Domain.Utility;
using Xunit;

namespace BleGolfSensorViewer.Tests.Infrastructure;

public class HexEncoderTests
{
    [Fact]
    public void ToHex_ReturnsUppercase()
    {
        var result = HexEncoder.ToHex(new byte[] { 0x0F, 0xA0, 0xB1 });
        Assert.Equal("0FA0B1", result);
    }

    [Fact]
    public void ToHex_EmptyArray_ReturnsEmptyString()
    {
        var result = HexEncoder.ToHex(Array.Empty<byte>());
        Assert.Equal(string.Empty, result);
    }
}
