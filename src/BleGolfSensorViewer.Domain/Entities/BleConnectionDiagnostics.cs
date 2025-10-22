using System.Collections.Generic;

namespace BleGolfSensorViewer.Domain.Entities;

/// <summary>
/// Captures diagnostics gathered while connecting to a BLE device.
/// </summary>
public sealed record BleConnectionDiagnostics(
    string? DeviceId,
    bool? InitialIsPaired,
    bool? InitialCanPair,
    bool? FinalIsPaired,
    bool? FinalCanPair,
    bool PairingAttempted,
    BlePairingStatus? PairingStatus,
    BlePairingProtectionLevel? PairingProtectionLevel,
    BleConnectionState ConnectionStatus,
    IReadOnlyList<string> Notes);
