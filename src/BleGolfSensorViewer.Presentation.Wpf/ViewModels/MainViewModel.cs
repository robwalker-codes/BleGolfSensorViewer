using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using BleGolfSensorViewer.Application.Contracts;
using BleGolfSensorViewer.Application.UseCases;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BleGolfSensorViewer.Presentation.Wpf.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ScanDevicesUseCase _scanDevicesUseCase;
    private readonly ConnectDeviceUseCase _connectDeviceUseCase;
    private readonly SubscribeNotificationsUseCase _subscribeNotificationsUseCase;
    private readonly DisconnectDeviceUseCase _disconnectDeviceUseCase;
    private readonly IProtocolDecoder _protocolDecoder;
    private readonly IBleDeviceConnector _deviceConnector;
    private readonly Dispatcher _dispatcher;

    private DiscoveredDevice? _selectedDevice;
    private string _status = "Idle";
    private readonly StringBuilder _logBuilder = new();
    private string _logText = string.Empty;
    private bool _isBusy;

    public MainViewModel(
        ScanDevicesUseCase scanDevicesUseCase,
        ConnectDeviceUseCase connectDeviceUseCase,
        SubscribeNotificationsUseCase subscribeNotificationsUseCase,
        DisconnectDeviceUseCase disconnectDeviceUseCase,
        IProtocolDecoder protocolDecoder,
        IBleDeviceConnector deviceConnector)
    {
        _scanDevicesUseCase = scanDevicesUseCase ?? throw new ArgumentNullException(nameof(scanDevicesUseCase));
        _connectDeviceUseCase = connectDeviceUseCase ?? throw new ArgumentNullException(nameof(connectDeviceUseCase));
        _subscribeNotificationsUseCase = subscribeNotificationsUseCase ?? throw new ArgumentNullException(nameof(subscribeNotificationsUseCase));
        _disconnectDeviceUseCase = disconnectDeviceUseCase ?? throw new ArgumentNullException(nameof(disconnectDeviceUseCase));
        _protocolDecoder = protocolDecoder ?? throw new ArgumentNullException(nameof(protocolDecoder));
        _deviceConnector = deviceConnector ?? throw new ArgumentNullException(nameof(deviceConnector));
        _dispatcher = Dispatcher.CurrentDispatcher;

        Devices = new ObservableCollection<DiscoveredDevice>();
        Measurements = new ObservableCollection<MeasurementRow>();

        _deviceConnector.ConnectionStateChanged += OnConnectionStateChanged;

        ScanCommand = new AsyncRelayCommand(ExecuteScanAsync, () => !_isBusy);
        ConnectCommand = new AsyncRelayCommand(ExecuteConnectAsync, CanConnect);
        DisconnectCommand = new AsyncRelayCommand(ExecuteDisconnectAsync, () => _deviceConnector.IsConnected && !_isBusy);
    }

    public ObservableCollection<DiscoveredDevice> Devices { get; }

    public ObservableCollection<MeasurementRow> Measurements { get; }

    public AsyncRelayCommand ScanCommand { get; }

    public AsyncRelayCommand ConnectCommand { get; }

    public AsyncRelayCommand DisconnectCommand { get; }

    public DiscoveredDevice? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                ConnectCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    private async Task ExecuteScanAsync()
    {
        if (_scanDevicesUseCase.IsScanning)
        {
            await _scanDevicesUseCase.StopAsync(CancellationToken.None);
            UpdateStatus("Idle");
            AppendLog("Scan stopped.");
            return;
        }

        Devices.Clear();
        UpdateStatus("Scanning...");
        AppendLog("Scan started.");
        await _scanDevicesUseCase.StartAsync(new ScanDevicesRequest(), OnDeviceDiscovered, CancellationToken.None);
    }

    private async Task ExecuteConnectAsync()
    {
        if (SelectedDevice is null)
        {
            AppendLog("Select a device before connecting.");
            return;
        }

        await StopScanIfRunningAsync();
        await RunBusyOperation(async () =>
        {
            try
            {
                UpdateStatus("Connecting...");
                var response = await _connectDeviceUseCase.ExecuteAsync(new ConnectDeviceRequest(SelectedDevice), CancellationToken.None);
                foreach (var message in response.Messages)
                {
                    AppendLog(message);
                }
                await _subscribeNotificationsUseCase.StartAsync(SelectedDevice.Id, OnMeasurementReceived, CancellationToken.None);
                UpdateStatus("Subscribed");
                AppendLog($"Connected to {SelectedDevice.Name}.");
            }
            catch (Exception ex)
            {
                UpdateStatus("Error");
                AppendLog($"Connection failed: {ex.Message}");
            }
        });
    }

    private async Task ExecuteDisconnectAsync()
    {
        await RunBusyOperation(async () =>
        {
            try
            {
                UpdateStatus("Disconnecting...");
                await _disconnectDeviceUseCase.ExecuteAsync(CancellationToken.None);
                UpdateStatus("Disconnected");
                AppendLog("Disconnected.");
            }
            catch (Exception ex)
            {
                UpdateStatus("Error");
                AppendLog($"Disconnect failed: {ex.Message}");
            }
        });
    }

    private bool CanConnect() => SelectedDevice is not null && !_deviceConnector.IsConnected && !_isBusy;

    private void OnDeviceDiscovered(DiscoveredDevice device)
    {
        _dispatcher.BeginInvoke(new Action(() =>
        {
            if (Devices.Any(d => d.Id.Equals(device.Id)))
            {
                return;
            }

            Devices.Add(device);
            AppendLog($"Discovered {device.Name} RSSI {device.Rssi}.");
        }));
    }

    private void OnMeasurementReceived(object? sender, Measurement measurement)
    {
        var decoded = _protocolDecoder.Decode(measurement);
        var row = MeasurementRow.FromDecodedMeasurement(decoded);

        _dispatcher.BeginInvoke(new Action(() =>
        {
            Measurements.Add(row);
        }));
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        _dispatcher.BeginInvoke(new Action(() =>
        {
            if (!isConnected)
            {
                UpdateStatus("Disconnected");
            }

            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
        }));
    }

    private async Task StopScanIfRunningAsync()
    {
        if (_scanDevicesUseCase.IsScanning)
        {
            await _scanDevicesUseCase.StopAsync(CancellationToken.None);
        }
    }

    private async Task RunBusyOperation(Func<Task> operation)
    {
        _isBusy = true;
        ScanCommand.NotifyCanExecuteChanged();
        ConnectCommand.NotifyCanExecuteChanged();
        DisconnectCommand.NotifyCanExecuteChanged();

        try
        {
            await operation();
        }
        finally
        {
            _isBusy = false;
            ScanCommand.NotifyCanExecuteChanged();
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
        }
    }

    private void UpdateStatus(string status)
    {
        Status = status;
    }

    public void AppendLog(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _logBuilder.AppendLine($"[{DateTimeOffset.Now:HH:mm:ss}] {line}");
        LogText = _logBuilder.ToString();
    }
}
