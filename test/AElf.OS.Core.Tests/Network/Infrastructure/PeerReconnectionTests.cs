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
            var endpoint = "127.0.0.1:5677";
            var utcNow = TimestampHelper.GetUtcNow();

            _reconnectionProvider.AddReconnectingPeer(endpoint, new ReconnectingPeer { Endpoint = endpoint, NextAttempt = utcNow });

            _reconnectionProvider.GetPeersReadyForReconnection(utcNow.AddSeconds(-1)).Count.ShouldBe(0);
            _reconnectionProvider.GetPeersReadyForReconnection(utcNow.AddSeconds(1)).Count.ShouldBe(1);
        }

        [Fact]
        public void Removed_ShouldNotBeInProvider()
        {
            var endpoint = "127.0.0.1:5677";
            var utcNow = TimestampHelper.GetUtcNow();

            _reconnectionProvider.AddReconnectingPeer(endpoint, new ReconnectingPeer { Endpoint = endpoint, NextAttempt = utcNow });
            _reconnectionProvider.RemoveReconnectionPeer(endpoint);

            _reconnectionProvider.GetPeersReadyForReconnection(utcNow.AddSeconds(1)).Count.ShouldBe(0);
        }
    }
}