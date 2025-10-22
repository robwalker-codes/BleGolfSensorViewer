namespace BleGolfSensorViewer.Domain.Entities;

/// <summary>
/// Represents the high-level outcome of a BLE pairing attempt.
/// </summary>
public enum BlePairingStatus
{
    Unknown = 0,
    Paired,
    AlreadyPaired,
    NotReady,
    Failed,
    AccessDenied,
    OperationInProgress,
    Rejected,
    Timeout,
    Cancelled,
    Unsupported,
}
