using System;

namespace BleGolfSensorViewer.Domain.Entities;

/// <summary>
/// Represents a BLE device discovered during scanning.
/// </summary>
public sealed class DiscoveredDevice
{
    public DiscoveredDevice(BleDeviceId id, string name, short rssi)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        Rssi = rssi;
    }

    public BleDeviceId Id { get; }

    public string Name { get; }

    public short Rssi { get; }

    public override string ToString() => $"{Name} ({Id}) RSSI: {Rssi}dBm";
}
