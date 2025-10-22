#if WINDOWS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BleGolfSensorViewer.Infrastructure.Ble.WinRt;

/// <summary>
/// WinRT-based device connector that understands pairing workflows.
/// </summary>
public sealed class BleDeviceConnectorWinRt : IBleDeviceConnector
{
    private BluetoothLEDevice? _device;

    public event EventHandler<bool>? ConnectionStateChanged;

    public BluetoothLEDevice? ConnectedDevice => _device;

    public bool IsConnected => _device?.ConnectionStatus == BluetoothConnectionStatus.Connected;

    public async Task<BleConnectionDiagnostics> ConnectAsync(BleDeviceId deviceId, CancellationToken cancellationToken)
    {
        if (deviceId is null)
        {
            throw new ArgumentNullException(nameof(deviceId));
        }

        if (_device is not null)
        {
            await DisposeCurrentDeviceAsync().ConfigureAwait(false);
        }

        var builder = new BleConnectionDiagnosticsBuilder();

        try
        {
            return await ConnectInternalAsync(deviceId, builder, cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            builder.AddNote($"Unauthorized access encountered: {ex.Message}");
            throw new UnauthorizedAccessException(
                "Access denied while opening the BLE device. Ensure the sensor is not connected elsewhere, enable Windows Location Services, and complete pairing in Windows Settings if prompted.",
                ex);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException("The supplied Bluetooth address is invalid.", nameof(deviceId), ex);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_device is null)
        {
            return;
        }

        await DisposeCurrentDeviceAsync().ConfigureAwait(false);
        RaiseConnectionStateChanged(false);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCurrentDeviceAsync().ConfigureAwait(false);
    }

    private async Task<BleConnectionDiagnostics> ConnectInternalAsync(
        BleDeviceId deviceId,
        BleConnectionDiagnosticsBuilder builder,
        CancellationToken cancellationToken)
    {
        PairingSessionResult? session = null;

        try
        {
            session = await ConnectWithPairingIfNeededAsync(deviceId.Address, cancellationToken, builder.AddNote)
                .ConfigureAwait(false);

            var device = session.Device;
            builder.DeviceId ??= device.DeviceId;
            builder.SetInitialState(session.InitialIsPaired, session.InitialCanPair);
            builder.SetFinalState(session.FinalIsPaired, session.FinalCanPair);

            if (session.Outcome.PairingAttempted)
            {
                builder.PairingAttempted = true;
                builder.PairingStatus = session.Outcome.Status;
                builder.PairingProtectionLevel = session.Outcome.ProtectionLevel;
            }

            builder.ConnectionStatus = MapConnectionStatus(device.ConnectionStatus);

            var diagnostics = builder.Build();
            AttachDevice(device);
            session = null;
            return diagnostics;
        }
        finally
        {
            session?.Device.Dispose();
        }
    }

    private async Task<PairingSessionResult> ConnectWithPairingIfNeededAsync(
        ulong address,
        CancellationToken cancellationToken,
        Action<string>? log)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            throw new InvalidOperationException("Device not found.");
        }

        var pairing = device.DeviceInformation?.Pairing;
        var initialIsPaired = pairing?.IsPaired;
        var initialCanPair = pairing?.CanPair;
        var outcome = PairingAttemptOutcome.NotAttempted();

        if (pairing is { IsPaired: false })
        {
            var basic = await pairing.PairAsync(DevicePairingProtectionLevel.Encryption)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);

            log?.Invoke($"PairAsync(basic) → {basic.Status}");

            var basicStatus = MapPairingStatus(basic.Status);
            outcome = PairingAttemptOutcome.Attempted(basicStatus, BlePairingProtectionLevel.Encryption);
            ThrowIfAccessDenied(basicStatus);

            if (!IsSuccessful(basicStatus))
            {
                if (pairing.CanPair && pairing.Custom is not null)
                {
                    var custom = await pairing.Custom.PairAsync(
                            DevicePairingKinds.ConfirmOnly,
                            DevicePairingProtectionLevel.Encryption)
                        .AsTask(cancellationToken)
                        .ConfigureAwait(false);

                    log?.Invoke($"PairAsync(custom) → {custom.Status}");

                    var customStatus = MapPairingStatus(custom.Status);
                    outcome = PairingAttemptOutcome.Attempted(customStatus, BlePairingProtectionLevel.Encryption);
                    ThrowIfAccessDenied(customStatus);

                    if (!IsSuccessful(customStatus))
                    {
                        throw new InvalidOperationException($"Pairing failed: {custom.Status}");
                    }

                    device.Dispose();
                    device = await BluetoothLEDevice.FromBluetoothAddressAsync(address)
                        .AsTask(cancellationToken)
                        .ConfigureAwait(false);

                    if (device is null)
                    {
                        throw new InvalidOperationException("Device not found after pairing.");
                    }

                    pairing = device.DeviceInformation?.Pairing;
                }
                else
                {
                    throw new InvalidOperationException($"Pairing failed: {basic.Status}");
                }
            }
        }

        var finalIsPaired = pairing?.IsPaired;
        var finalCanPair = pairing?.CanPair;

        return new PairingSessionResult(device, initialIsPaired, initialCanPair, finalIsPaired, finalCanPair, outcome);
    }

    private static void ThrowIfAccessDenied(BlePairingStatus status)
    {
        if (status == BlePairingStatus.AccessDenied)
        {
            throw new UnauthorizedAccessException("Pairing was denied by the peripheral.");
        }
    }

    private static bool IsSuccessful(BlePairingStatus status) =>
        status is BlePairingStatus.Paired or BlePairingStatus.AlreadyPaired;

    private void AttachDevice(BluetoothLEDevice device)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _device.ConnectionStatusChanged += OnConnectionStatusChanged;
        RaiseConnectionStateChanged(IsConnected);
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        RaiseConnectionStateChanged(IsConnected);
    }

    private void RaiseConnectionStateChanged(bool isConnected)
    {
        ConnectionStateChanged?.Invoke(this, isConnected);
    }

    private async Task DisposeCurrentDeviceAsync()
    {
        if (_device is null)
        {
            return;
        }

        _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _device.Dispose();
        await Task.Yield();
        _device = null;
    }

    private static BleConnectionState MapConnectionStatus(BluetoothConnectionStatus status) => status switch
    {
        BluetoothConnectionStatus.Connected => BleConnectionState.Connected,
        BluetoothConnectionStatus.Disconnected => BleConnectionState.Disconnected,
        _ => BleConnectionState.Unknown,
    };

    private static BlePairingStatus MapPairingStatus(DevicePairingResultStatus status) => status switch
    {
        DevicePairingResultStatus.Paired => BlePairingStatus.Paired,
        DevicePairingResultStatus.AlreadyPaired => BlePairingStatus.AlreadyPaired,
        DevicePairingResultStatus.NotReadyToPair => BlePairingStatus.NotReady,
        DevicePairingResultStatus.NotPaired => BlePairingStatus.NotPaired,
        DevicePairingResultStatus.AccessDenied => BlePairingStatus.AccessDenied,
        DevicePairingResultStatus.ConnectionRejected => BlePairingStatus.ConnectionRejected,
        DevicePairingResultStatus.TooManyConnections => BlePairingStatus.Failed,
        DevicePairingResultStatus.HardwareFailure => BlePairingStatus.Failed,
        DevicePairingResultStatus.AuthenticationTimeout => BlePairingStatus.Failed,
        DevicePairingResultStatus.AuthenticationNotAllowed => BlePairingStatus.Failed,
        DevicePairingResultStatus.AuthenticationFailure => BlePairingStatus.Failed,
        DevicePairingResultStatus.NoSupportedProfiles => BlePairingStatus.Failed,
        DevicePairingResultStatus.ProtectionLevelCouldNotBeMet => BlePairingStatus.Failed,
        DevicePairingResultStatus.InvalidCeremonyData => BlePairingStatus.Failed,
        DevicePairingResultStatus.OperationAlreadyInProgress => BlePairingStatus.OperationInProgress,
        DevicePairingResultStatus.RequiredHandlerNotRegistered => BlePairingStatus.Failed,
        DevicePairingResultStatus.RejectedByHandler => BlePairingStatus.Rejected,
        DevicePairingResultStatus.RemoteDeviceHasAssociation => BlePairingStatus.Failed,
        DevicePairingResultStatus.PairingCanceled => BlePairingStatus.Canceled,
        DevicePairingResultStatus.Failed => BlePairingStatus.Failed,
        _ => BlePairingStatus.Unknown,
    };

    private sealed class PairingSessionResult
    {
        public PairingSessionResult(
            BluetoothLEDevice device,
            bool? initialIsPaired,
            bool? initialCanPair,
            bool? finalIsPaired,
            bool? finalCanPair,
            PairingAttemptOutcome outcome)
        {
            Device = device;
            InitialIsPaired = initialIsPaired;
            InitialCanPair = initialCanPair;
            FinalIsPaired = finalIsPaired;
            FinalCanPair = finalCanPair;
            Outcome = outcome;
        }

        public BluetoothLEDevice Device { get; }

        public bool? InitialIsPaired { get; }

        public bool? InitialCanPair { get; }

        public bool? FinalIsPaired { get; }

        public bool? FinalCanPair { get; }

        public PairingAttemptOutcome Outcome { get; }
    }

    private readonly struct PairingAttemptOutcome
    {
        private PairingAttemptOutcome(bool attempted, BlePairingStatus? status, BlePairingProtectionLevel? level)
        {
            PairingAttempted = attempted;
            Status = status;
            ProtectionLevel = level;
        }

        public bool PairingAttempted { get; }

        public BlePairingStatus? Status { get; }

        public BlePairingProtectionLevel? ProtectionLevel { get; }

        public static PairingAttemptOutcome NotAttempted() => new(false, null, null);

        public static PairingAttemptOutcome Attempted(BlePairingStatus status, BlePairingProtectionLevel level)
        {
            return new PairingAttemptOutcome(true, status, level);
        }
    }

    private sealed class BleConnectionDiagnosticsBuilder
    {
        private readonly List<string> _notes = new();

        public string? DeviceId { get; set; }

        public bool? InitialIsPaired { get; private set; }

        public bool? InitialCanPair { get; private set; }

        public bool? FinalIsPaired { get; private set; }

        public bool? FinalCanPair { get; private set; }

        public bool PairingAttempted { get; set; }

        public BlePairingStatus? PairingStatus { get; set; }

        public BlePairingProtectionLevel? PairingProtectionLevel { get; set; }

        public BleConnectionState ConnectionStatus { get; set; } = BleConnectionState.Unknown;

        public void SetInitialState(bool? isPaired, bool? canPair)
        {
            InitialIsPaired ??= isPaired;
            InitialCanPair ??= canPair;
        }

        public void SetFinalState(bool? isPaired, bool? canPair)
        {
            FinalIsPaired = isPaired ?? FinalIsPaired ?? InitialIsPaired;
            FinalCanPair = canPair ?? FinalCanPair ?? InitialCanPair;
        }

        public void AddNote(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return;
            }

            _notes.Add(note);
        }

        public BleConnectionDiagnostics Build()
        {
            AddSummaryNotes();
            return new BleConnectionDiagnostics(
                DeviceId,
                InitialIsPaired,
                InitialCanPair,
                FinalIsPaired,
                FinalCanPair,
                PairingAttempted,
                PairingStatus,
                PairingProtectionLevel,
                ConnectionStatus,
                _notes.AsReadOnly());
        }

        private void AddSummaryNotes()
        {
            AddNote($"Pairing state before connect - IsPaired: {FormatBool(InitialIsPaired)}, CanPair: {FormatBool(InitialCanPair)}");
            AddNote($"Pairing state after connect - IsPaired: {FormatBool(FinalIsPaired)}, CanPair: {FormatBool(FinalCanPair)}");
            AddNote($"ConnectionStatus: {ConnectionStatus}");

            if (PairingAttempted && PairingStatus is not null)
            {
                AddNote($"Pairing attempt result: {PairingStatus} (Protection: {PairingProtectionLevel})");
            }
        }

        private static string FormatBool(bool? value) => value switch
        {
            true => "True",
            false => "False",
            _ => "Unknown",
        };
    }
}
#endif
