using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Application.UseCases;
using BleGolfSensorViewer.Domain.Abstractions;
using BleGolfSensorViewer.Domain.Entities;
using Moq;
using Xunit;

namespace BleGolfSensorViewer.Tests.Application;

public class SubscribeNotificationsUseCaseTests
{
    [Fact]
    public async Task StartAsync_ForwardsMeasurements()
    {
        var serviceMock = new Mock<IGattSubscriptionService>();
        var useCase = new SubscribeNotificationsUseCase(serviceMock.Object);
        Measurement? received = null;

        await useCase.StartAsync(new BleDeviceId(1), (_, measurement) => received = measurement, CancellationToken.None);

        var measurement = new Measurement(System.DateTimeOffset.UtcNow, System.Guid.NewGuid(), System.Guid.NewGuid(), new byte[] { 1, 2 });
        serviceMock.Raise(s => s.MeasurementReceived += null!, measurement);

        Assert.Equal(measurement, received);
    }

    [Fact]
    public async Task StopAsync_UnsubscribesHandler()
    {
        var serviceMock = new Mock<IGattSubscriptionService>();
        var useCase = new SubscribeNotificationsUseCase(serviceMock.Object);

        await useCase.StartAsync(new BleDeviceId(1), (_, _) => { }, CancellationToken.None);
        await useCase.StopAsync(CancellationToken.None);

        serviceMock.Verify(s => s.UnsubscribeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
