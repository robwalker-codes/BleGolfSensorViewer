using System;
using System.Buffers.Binary;

namespace BleGolfSensorViewer.Infrastructure.Ble.Protocol.Parsers;

/// <summary>
/// Helper for reading little-endian floats from byte spans.
/// </summary>
public static class FloatParser
{
    public static float ReadLittleEndianFloat(ReadOnlySpan<byte> buffer, int offset)
    {
        if (buffer.Length < offset + sizeof(float))
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Buffer too small to read float.");
        }

        var intValue = BinaryPrimitives.ReadInt32LittleEndian(buffer[offset..]);
        return BitConverter.Int32BitsToSingle(intValue);
    }
}
