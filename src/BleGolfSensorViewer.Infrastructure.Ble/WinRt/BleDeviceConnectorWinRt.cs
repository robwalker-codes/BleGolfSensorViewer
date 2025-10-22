using System;
using System.Collections.Generic;
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
            return await ConnectInternalAsync(deviceId, builder, cancellationToken, allowFallback: true).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            builder.AddNote($"Unauthorized access encountered: {ex.Message}");
            throw new UnauthorizedAccessException(
                "Access denied. Likely needs pairing or device is connected elsewhere. Close mobile apps, power-cycle the sensor, and try again. If it persists, pairing is attempted programmatically.",
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
        CancellationToken cancellationToken,
        bool allowFallback)
    {
        BluetoothLEDevice? device = null;

        try
        {
            device = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceId.Address)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);

            if (device is null)
            {
                throw new InvalidOperationException("Bluetooth device could not be created.");
            }

            var pairingContext = CreatePairingContext(device);
            builder.DeviceId ??= pairingContext?.Id ?? device.DeviceId;
            builder.SetInitialState(pairingContext);

            var outcome = await EnsurePairedAsync(pairingContext, cancellationToken, builder.AddNote).ConfigureAwait(false);

            if (outcome.PairingAttempted)
            {
                builder.PairingAttempted = true;
                builder.PairingStatus = outcome.Status;
                builder.PairingProtectionLevel = outcome.ProtectionLevel;
            }

            if (outcome.PairingAttempted && outcome.PairingSucceeded)
            {
                device.Dispose();
                device = await BluetoothLEDevice.FromBluetoothAddressAsync(deviceId.Address)
                    .AsTask(cancellationToken)
                    .ConfigureAwait(false);

                if (device is null)
                {
                    throw new InvalidOperationException("Bluetooth device could not be created after pairing.");
                }

                pairingContext = CreatePairingContext(device);
            }

            builder.SetFinalState(pairingContext);
            builder.ConnectionStatus = MapConnectionStatus(device.ConnectionStatus);

            var diagnostics = builder.Build();
            AttachDevice(device);
            device = null;
            return diagnostics;
        }
        catch (UnauthorizedAccessException) when (allowFallback)
        {
            device?.Dispose();
            var fallback = await TryPairUsingDeviceInformationAsync(deviceId, builder, cancellationToken).ConfigureAwait(false);
            if (fallback is not null)
            {
                return fallback;
            }

            throw;
        }
        finally
        {
            device?.Dispose();
        }
    }

    private async Task<BleConnectionDiagnostics?> TryPairUsingDeviceInformationAsync(
        BleDeviceId deviceId,
        BleConnectionDiagnosticsBuilder builder,
        CancellationToken cancellationToken)
    {
        builder.AddNote("Attempting DeviceInformation pairing fallback.");

        var selector = BluetoothLEDevice.GetDeviceSelectorFromBluetoothAddress(deviceId.Address);
        var devices = await DeviceInformation.FindAllAsync(selector)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);

        foreach (var candidate in devices)
        {
            var context = new DeviceInformationPairingContext(candidate);
            builder.AddNote($"Evaluating DeviceInformation candidate: {context.Id} (IsPaired={context.IsPaired}, CanPair={context.CanPair}).");
            if (context.CanPair != true)
            {
                continue;
            }

            builder.DeviceId ??= context.Id;
            builder.SetInitialState(context);

            var outcome = await EnsurePairedAsync(context, cancellationToken, builder.AddNote).ConfigureAwait(false);

            if (outcome.PairingAttempted)
            {
                builder.PairingAttempted = true;
                builder.PairingStatus = outcome.Status;
                builder.PairingProtectionLevel = outcome.ProtectionLevel;
            }

            if (outcome.PairingAttempted && outcome.PairingSucceeded)
            {
                builder.AddNote("DeviceInformation fallback paired the device.");
                return await ConnectInternalAsync(deviceId, builder, cancellationToken, allowFallback: false)
                    .ConfigureAwait(false);
            }
        }

        builder.AddNote("DeviceInformation fallback did not pair the device.");
        return null;
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

    private static IDevicePairingContext? CreatePairingContext(BluetoothLEDevice device)
    {
        if (device.DeviceInformation is null)
        {
            return null;
        }

        return new DeviceInformationPairingContext(device.DeviceInformation);
    }

    internal static async Task<PairingAttemptOutcome> EnsurePairedAsync(
        IDevicePairingContext? pairingContext,
        CancellationToken cancellationToken,
        Action<string>? log)
    {
        if (pairingContext is null)
        {
            log?.Invoke("Pairing context unavailable.");
            return PairingAttemptOutcome.NotAttempted(false);
        }

        if (pairingContext.IsPaired == true)
        {
            log?.Invoke("Device already paired.");
            return PairingAttemptOutcome.NotAttempted(true);
        }

        if (pairingContext.CanPair != true)
        {
            log?.Invoke($"Pairing not supported. CanPair={pairingContext.CanPair}.");
            return PairingAttemptOutcome.NotAttempted(pairingContext.IsPaired == true);
        }

        var first = await AttemptPairingAsync(pairingContext, BlePairingProtectionLevel.None, cancellationToken, log)
            .ConfigureAwait(false);
        if (first.PairingSucceeded)
        {
            return first;
        }

        var second = await AttemptPairingAsync(pairingContext, BlePairingProtectionLevel.Encryption, cancellationToken, log)
            .ConfigureAwait(false);
        if (second.PairingSucceeded)
        {
            return second;
        }

        throw new InvalidOperationException($"Pairing failed: {second.Status}");
    }

    private static async Task<PairingAttemptOutcome> AttemptPairingAsync(
        IDevicePairingContext context,
        BlePairingProtectionLevel level,
        CancellationToken cancellationToken,
        Action<string>? log)
    {
        var status = await context.PairAsync(level, cancellationToken).ConfigureAwait(false);
        log?.Invoke($"Pairing attempt using {level} returned {status}.");

        if (status == BlePairingStatus.AccessDenied)
        {
            throw new UnauthorizedAccessException("Pairing was denied by the peripheral.");
        }

        return PairingAttemptOutcome.Attempted(status, level);
    }

    private static BleConnectionState MapConnectionStatus(BluetoothConnectionStatus status) => status switch
    {
        BluetoothConnectionStatus.Connected => BleConnectionState.Connected,
        BluetoothConnectionStatus.Disconnected => BleConnectionState.Disconnected,
        _ => BleConnectionState.Unknown,
    };

    internal interface IDevicePairingContext
    {
        string Id { get; }

        bool? IsPaired { get; }

        bool? CanPair { get; }

        Task<BlePairingStatus> PairAsync(BlePairingProtectionLevel protectionLevel, CancellationToken cancellationToken);
    }

    private sealed class DeviceInformationPairingContext : IDevicePairingContext
    {
        private readonly DeviceInformation _deviceInformation;

        public DeviceInformationPairingContext(DeviceInformation deviceInformation)
        {
            _deviceInformation = deviceInformation ?? throw new ArgumentNullException(nameof(deviceInformation));
        }

        public string Id => _deviceInformation.Id;

        public bool? IsPaired => _deviceInformation.Pairing?.IsPaired;

        public bool? CanPair => _deviceInformation.Pairing?.CanPair;

        public async Task<BlePairingStatus> PairAsync(
            BlePairingProtectionLevel protectionLevel,
            CancellationToken cancellationToken)
        {
            var custom = _deviceInformation.Pairing?.Custom;
            if (custom is null)
            {
                return BlePairingStatus.Unsupported;
            }

            var winRtLevel = protectionLevel switch
            {
                BlePairingProtectionLevel.Encryption => DevicePairingProtectionLevel.Encryption,
                _ => DevicePairingProtectionLevel.None,
            };

            var result = await custom.PairAsync(DevicePairingKinds.ConfirmOnly, winRtLevel)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);

            return MapPairingStatus(result.Status);
        }
    }

    internal readonly struct PairingAttemptOutcome
    {
        private PairingAttemptOutcome(bool attempted, bool succeeded, BlePairingStatus? status, BlePairingProtectionLevel? level)
        {
            PairingAttempted = attempted;
            PairingSucceeded = succeeded;
            Status = status;
            ProtectionLevel = level;
        }

        public bool PairingAttempted { get; }

        public bool PairingSucceeded { get; }

        public BlePairingStatus? Status { get; }

        public BlePairingProtectionLevel? ProtectionLevel { get; }

        public static PairingAttemptOutcome NotAttempted(bool succeeded) => new(false, succeeded, null, null);

        public static PairingAttemptOutcome Attempted(BlePairingStatus status, BlePairingProtectionLevel level)
        {
            return new PairingAttemptOutcome(true, IsSuccessful(status), status, level);
        }

        private static bool IsSuccessful(BlePairingStatus status) => status is BlePairingStatus.Paired or BlePairingStatus.AlreadyPaired;
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

        public void SetInitialState(IDevicePairingContext? context)
        {
            if (context is null)
            {
                return;
            }

            InitialIsPaired ??= context.IsPaired;
            InitialCanPair ??= context.CanPair;
        }

        public void SetFinalState(IDevicePairingContext? context)
        {
            if (context is null)
            {
                FinalIsPaired = FinalIsPaired ?? InitialIsPaired;
                FinalCanPair = FinalCanPair ?? InitialCanPair;
                return;
            }

            FinalIsPaired = context.IsPaired;
            FinalCanPair = context.CanPair;
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

    private static BlePairingStatus MapPairingStatus(DevicePairingResultStatus status) => status switch
    {
        DevicePairingResultStatus.Paired => BlePairingStatus.Paired,
        DevicePairingResultStatus.AlreadyPaired => BlePairingStatus.AlreadyPaired,
        DevicePairingResultStatus.NotReadyToPair => BlePairingStatus.NotReady,
        DevicePairingResultStatus.Failed => BlePairingStatus.Failed,
        DevicePairingResultStatus.AccessDenied => BlePairingStatus.AccessDenied,
        DevicePairingResultStatus.OperationAlreadyInProgress => BlePairingStatus.OperationInProgress,
        DevicePairingResultStatus.RejectedByHandler => BlePairingStatus.Rejected,
        DevicePairingResultStatus.Timeout => BlePairingStatus.Timeout,
        DevicePairingResultStatus.PairedWithDifferentIdentity => BlePairingStatus.Failed,
        DevicePairingResultStatus.NotSupported => BlePairingStatus.Unsupported,
        DevicePairingResultStatus.FailedAuthentication => BlePairingStatus.Failed,
        DevicePairingResultStatus.FailedWithPendingAuthentication => BlePairingStatus.Failed,
        DevicePairingResultStatus.FailedWithCouldNotPair => BlePairingStatus.Failed,
        DevicePairingResultStatus.FailedWithTransportConfiguration => BlePairingStatus.Failed,
        DevicePairingResultStatus.Canceled => BlePairingStatus.Cancelled,
        _ => BlePairingStatus.Unknown,
    };
}

