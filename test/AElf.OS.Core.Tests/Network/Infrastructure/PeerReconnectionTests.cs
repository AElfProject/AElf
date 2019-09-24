using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerReconnectionTests : NetworkInfrastructureTestBase
    {
        private readonly IPeerReconnectionStateProvider _reconnectionProvider;

        public PeerReconnectionTests()
        {
            _reconnectionProvider = GetRequiredService<IPeerReconnectionStateProvider>();
        }
        
        [Fact]
        public void AddConnecting_ShouldAddPeers()
        {
            var utcNow = TimestampHelper.GetUtcNow();

            _reconnectionProvider.AddReconnectingPeer("a-peer", new ReconnectingPeer { Endpoint = "a-peer", NextAttempt = utcNow });

            _reconnectionProvider.GetPeersReadyForReconnection(utcNow.AddSeconds(-1)).Count.ShouldBe(0);
            _reconnectionProvider.GetPeersReadyForReconnection(utcNow.AddSeconds(1)).Count.ShouldBe(1);
        }
    }
}