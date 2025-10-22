namespace BleGolfSensorViewer.Application.Contracts;

/// <summary>
/// Parameters for scanning BLE devices.
/// </summary>
/// <param name="ClearExisting">Indicates if the current device list should be cleared before scanning.</param>
public sealed record ScanDevicesRequest(bool ClearExisting = true);
