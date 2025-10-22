using System;

namespace BleGolfSensorViewer.Domain.Errors;

/// <summary>
/// Provides error messages and exception factory helpers for BLE operations.
/// </summary>
public static class BleErrors
{
    public static InvalidOperationException AlreadyConnected() =>
        new("A device is already connected. Disconnect before connecting another device.");

    public static InvalidOperationException NotConnected() =>
        new("No device is connected.");

    public static InvalidOperationException ConnectionFailed(string reason) =>
        new($"Failed to connect to the device: {reason}");
}
