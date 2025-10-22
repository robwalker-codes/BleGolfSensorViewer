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
    NotPaired,
    Failed,
    AccessDenied,
    ConnectionRejected,
    OperationInProgress,
    Rejected,
    Timeout,
    Canceled,
    Unsupported,
}
