using System;

namespace BleGolfSensorViewer.Infrastructure.Ble.Protocol;

/// <summary>
/// Defines known service and characteristic UUIDs.
/// TODO: Replace placeholder GUIDs with vendor-specific values.
/// </summary>
public static class KnownUuids
{
    public static readonly Guid MotionService = Guid.Parse("00000000-0000-0000-0000-000000000001"); // TODO: Replace with actual PhiGolf motion service UUID.
    public static readonly Guid MotionDataCharacteristic = Guid.Parse("00000000-0000-0000-0000-000000000002"); // TODO: Replace with actual PhiGolf motion data characteristic UUID.
}
