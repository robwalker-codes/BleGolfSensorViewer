using System;

namespace BleGolfSensorViewer.Domain.Entities;

/// <summary>
/// Represents the unique identifier for a BLE device.
/// </summary>
public sealed class BleDeviceId : IEquatable<BleDeviceId>
{
    public BleDeviceId(ulong address)
    {
        Address = address;
    }

    public ulong Address { get; }

    public override bool Equals(object? obj) => obj is BleDeviceId other && Equals(other);

    public bool Equals(BleDeviceId? other) => other is not null && Address == other.Address;

    public override int GetHashCode() => Address.GetHashCode();

    public override string ToString() => $"0x{Address:X}";
}
