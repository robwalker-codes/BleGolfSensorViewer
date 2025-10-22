using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Application.Contracts;
using BleGolfSensorViewer.Application.UseCases;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;
using Moq;
using Xunit;

namespace BleGolfSensorViewer.Tests.Application;

public class ScanDevicesUseCaseTests
{
    [Fact]
    public async Task StartAsync_ForwardsDiscoveredDevices()
    {
        var scannerMock = new Mock<IBleScanner>();
        var useCase = new ScanDevicesUseCase(scannerMock.Object);
        var discovered = new List<DiscoveredDevice>();

        await useCase.StartAsync(new ScanDevicesRequest(), device => discovered.Add(device), CancellationToken.None);

        var device = new DiscoveredDevice(new BleDeviceId(1), "Test", -50);
        scannerMock.Raise(s => s.DeviceDiscovered += null!, device);

        Assert.Contains(device, discovered);
    }

    [Fact]
    public async Task StopAsync_StopsScanningWhenActive()
    {
        var scannerMock = new Mock<IBleScanner>();
        var useCase = new ScanDevicesUseCase(scannerMock.Object);

        await useCase.StartAsync(new ScanDevicesRequest(), _ => { }, CancellationToken.None);
        await useCase.StopAsync(CancellationToken.None);

        scannerMock.Verify(s => s.StopScanningAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
