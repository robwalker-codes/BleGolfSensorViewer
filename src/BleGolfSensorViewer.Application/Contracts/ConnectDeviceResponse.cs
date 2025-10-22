using System.Collections.Generic;
using BleGolfSensorViewer.Domain.Entities;

namespace BleGolfSensorViewer.Application.Contracts;

/// <summary>
/// Represents the response returned after attempting to connect to a device.
/// </summary>
public sealed record ConnectDeviceResponse(BleConnectionDiagnostics Diagnostics, IReadOnlyList<string> Messages);
