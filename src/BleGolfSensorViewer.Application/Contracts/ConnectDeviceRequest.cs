using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Application.Contracts;

/// <summary>
/// Parameters for connecting to a BLE device.
/// </summary>
/// <param name="Device">The device to connect to.</param>
public sealed record ConnectDeviceRequest(DiscoveredDevice Device);
