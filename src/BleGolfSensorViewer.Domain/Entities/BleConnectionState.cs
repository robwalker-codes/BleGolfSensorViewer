namespace BleGolfSensorViewer.Domain.Entities;

/// <summary>
/// Represents the simplified connection state for a BLE device.
/// </summary>
public enum BleConnectionState
{
    Unknown = 0,
    Disconnected,
    Connected,
}
