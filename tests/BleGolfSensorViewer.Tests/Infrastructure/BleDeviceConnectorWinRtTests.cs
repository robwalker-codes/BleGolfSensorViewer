using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BleGolfSensorViewer.Domain.Entities;
using BleGolfSensorViewer.Infrastructure.Ble.WinRt;
using Xunit;

namespace BleGolfSensorViewer.Tests.Infrastructure;

public class BleDeviceConnectorWinRtTests
{
    [Fact]
    public async Task EnsurePairedAsync_WhenNotPairedAndCanPair_AttemptsPairing()
    {
        var context = new FakePairingContext(isPaired: false, canPair: true, BlePairingStatus.Paired);
        var logs = new List<string>();

        var outcome = await BleDeviceConnectorWinRt.EnsurePairedAsync(context, CancellationToken.None, logs.Add);

        Assert.True(outcome.PairingAttempted);
        Assert.True(outcome.PairingSucceeded);
        Assert.Equal(BlePairingProtectionLevel.None, outcome.ProtectionLevel);
        Assert.Equal(new[] { BlePairingProtectionLevel.None }, context.RequestedLevels);
    }

    [Fact]
    public async Task EnsurePairedAsync_WhenPairingReportsAlreadyPaired_Completes()
    {
        var context = new FakePairingContext(isPaired: false, canPair: true, BlePairingStatus.AlreadyPaired);

        var outcome = await BleDeviceConnectorWinRt.EnsurePairedAsync(context, CancellationToken.None, _ => { });

        Assert.True(outcome.PairingAttempted);
        Assert.True(outcome.PairingSucceeded);
        Assert.Equal(BlePairingProtectionLevel.None, outcome.ProtectionLevel);
        Assert.Single(context.RequestedLevels);
        Assert.Equal(BlePairingProtectionLevel.None, context.RequestedLevels.First());
    }

    private sealed class FakePairingContext : BleDeviceConnectorWinRt.IDevicePairingContext
    {
        private readonly Queue<BlePairingStatus> _results = new();

        public FakePairingContext(bool? isPaired, bool? canPair, params BlePairingStatus[] results)
        {
            IsPaired = isPaired;
            CanPair = canPair;
            foreach (var result in results)
            {
                _results.Enqueue(result);
            }
        }

        public string Id => "fake";

        public bool? IsPaired { get; }

        public bool? CanPair { get; }

        public List<BlePairingProtectionLevel> RequestedLevels { get; } = new();

        public Task<BlePairingStatus> PairAsync(BlePairingProtectionLevel protectionLevel, CancellationToken cancellationToken)
        {
            RequestedLevels.Add(protectionLevel);
            return Task.FromResult(_results.Count > 0 ? _results.Dequeue() : BlePairingStatus.Unknown);
        }
    }
}
