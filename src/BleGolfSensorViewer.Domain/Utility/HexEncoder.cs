using System;
using System.Text;

namespace BleGolfSensorViewer.Domain.Utility;

/// <summary>
/// Provides helpers for converting byte arrays to hexadecimal strings.
/// </summary>
public static class HexEncoder
{
    public static string ToHex(byte[] bytes)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            builder.AppendFormat("{0:X2}", b);
        }

        return builder.ToString();
    }
}
