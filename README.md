# BleGolfSensorViewer

BleGolfSensorViewer is a .NET 8 WPF application that connects to Bluetooth Low Energy (BLE) golf swing sensors, subscribes to their GATT notifications, and renders decoded swing metrics in real time. The solution follows Clean Architecture and SOLID principles to keep responsibilities isolated across domain, application, infrastructure, and presentation layers.

## Projects

- **BleGolfSensorViewer.Domain** – Pure domain model containing entities, value objects, service contracts, and utilities.
- **BleGolfSensorViewer.Application** – Use cases orchestrating high-level workflows through the domain abstractions.
- **BleGolfSensorViewer.Infrastructure.Ble** – WinRT-based BLE implementations and protocol decoding registry.
- **BleGolfSensorViewer.Presentation.Wpf** – MVVM WPF client with dependency injection wiring and UI views.
- **BleGolfSensorViewer.Tests** – xUnit test suite with Moq-powered unit tests for application logic, protocol decoding, and utilities.

## Building

```bash
dotnet build BleGolfSensorViewer.sln
```

> **Note:** The container used to generate this repository does not include the .NET SDK. Run the command on a Windows 11 machine with the .NET 8 SDK installed.

## Running

```bash
dotnet run --project src/BleGolfSensorViewer.Presentation.Wpf/BleGolfSensorViewer.Presentation.Wpf.csproj
```

### Permissions

The WPF application manifest enables the Bluetooth Generic Attribute Profile device capability. Windows will prompt for Bluetooth access the first time the app runs.

## Usage

1. Launch the app.
2. Click **Scan** to discover nearby BLE golf sensors. Discovered devices appear in the drop-down list.
3. Select a device and click **Connect**. The app will connect, enumerate notify/indicate characteristics, and subscribe for updates.
4. Measurements flow into the table in real time with decoded fields and raw hexadecimal payloads.
5. Click **Disconnect** to stop streaming and release BLE resources.

## Protocol configuration

The protocol registry ships with placeholder GUIDs and illustrative PhiGolf payload parsing logic. Replace the constants in `KnownUuids.cs` and adjust `ExamplePhigolfParsers.cs` to match your device’s actual UUIDs and packet schema.

## Testing

```bash
dotnet test BleGolfSensorViewer.sln
```

## Known limitations

- WinRT BLE APIs require Windows 10 version 19041 or later.
- The placeholder UUIDs must be replaced with vendor-specific identifiers for real hardware.
- UI auto-scrolling and command states rely on the WPF dispatcher; long-running operations should remain asynchronous.

## Contributing

Pull requests and issues are welcome. Please ensure that new code remains covered by tests and respects the existing architecture.
