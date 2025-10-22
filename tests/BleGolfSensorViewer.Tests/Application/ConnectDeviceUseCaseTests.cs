using System;
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

        await useCase.ExecuteAsync(request, CancellationToken.None);

        connectorMock.Verify(c => c.ConnectAsync(request.Device.Id, It.IsAny<CancellationToken>()), Times.Once);
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
}
