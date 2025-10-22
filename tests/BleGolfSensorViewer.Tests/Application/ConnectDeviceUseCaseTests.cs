using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Application.Contracts;
using BleGolfSensorViewer.Application.UseCases;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;
using BleGolfSensorViewer.Domain.Errors;
using Moq;
using Xunit;

namespace BleGolfSensorViewer.Tests.Application;

public class ConnectDeviceUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CallsConnector()
    {
        var connectorMock = new Mock<IBleDeviceConnector>();
        connectorMock.SetupGet(c => c.IsConnected).Returns(false);
        var useCase = new ConnectDeviceUseCase(connectorMock.Object);
        var request = new ConnectDeviceRequest(new DiscoveredDevice(new BleDeviceId(1), "Device", -40));

        var diagnostics = new BleConnectionDiagnostics(
            DeviceId: "device",
            InitialIsPaired: true,
            InitialCanPair: true,
            FinalIsPaired: true,
            FinalCanPair: true,
            PairingAttempted: false,
            PairingStatus: null,
            PairingProtectionLevel: null,
            ConnectionStatus: BleConnectionState.Connected,
            Notes: new List<string> { "note" });

        connectorMock
            .Setup(c => c.ConnectAsync(request.Device.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(diagnostics);

        var response = await useCase.ExecuteAsync(request, CancellationToken.None);

        connectorMock.Verify(c => c.ConnectAsync(request.Device.Id, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("note", response.Messages);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAlreadyConnected_Throws()
    {
        var connectorMock = new Mock<IBleDeviceConnector>();
        connectorMock.SetupGet(c => c.IsConnected).Returns(true);
        var useCase = new ConnectDeviceUseCase(connectorMock.Object);
        var request = new ConnectDeviceRequest(new DiscoveredDevice(new BleDeviceId(1), "Device", -40));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal(BleErrors.AlreadyConnected().Message, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenConnectorThrowsUnauthorized_MapsToGuidance()
    {
        var connectorMock = new Mock<IBleDeviceConnector>();
        connectorMock.SetupGet(c => c.IsConnected).Returns(false);
        connectorMock
            .Setup(c => c.ConnectAsync(It.IsAny<BleDeviceId>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        var useCase = new ConnectDeviceUseCase(connectorMock.Object);
        var request = new ConnectDeviceRequest(new DiscoveredDevice(new BleDeviceId(1), "Device", -40));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(request, CancellationToken.None));

        Assert.Contains("Enable Windows Location", ex.Message);
    }
}
