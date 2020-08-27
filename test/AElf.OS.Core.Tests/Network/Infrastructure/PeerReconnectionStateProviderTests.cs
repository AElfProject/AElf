using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.OS.Network.Infrastructure;
using AElf.Sdk.CSharp;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Infrastructure
{
    public class PeerReconnectionStateProviderTests : NetworkInfrastructureTestBase
    {
        private readonly IPeerReconnectionStateProvider _reconnectionProvider;

        public PeerReconnectionStateProviderTests()
        {
            _reconnectionProvider = GetRequiredService<IPeerReconnectionStateProvider>();
        }
        
        [Fact]
        public void AddReconnectingPeer_Test()
        {
            var endpoint = "127.0.0.1:5677";
            var utcNow = TimestampHelper.GetUtcNow();

            _reconnectionProvider
                .AddReconnectingPeer(endpoint, new ReconnectingPeer {Endpoint = endpoint, NextAttempt = utcNow})
                .ShouldBeTrue();
            
            var reconnectingPeer = _reconnectionProvider.GetReconnectingPeer(endpoint);
            reconnectingPeer.Endpoint.ShouldBe(endpoint);
            reconnectingPeer.NextAttempt.ShouldBe(utcNow);
            
            _reconnectionProvider
                .AddReconnectingPeer(endpoint, new ReconnectingPeer {Endpoint = endpoint, NextAttempt = utcNow})
                .ShouldBeFalse();
        }

        [Fact]
        public void GetPeersReadyForReconnection_Test()
        {
            var endpoint1 = "127.0.0.1:5671";
            var utcNow1 = TimestampHelper.GetUtcNow();
            _reconnectionProvider
                .AddReconnectingPeer(endpoint1, new ReconnectingPeer {Endpoint = endpoint1, NextAttempt = utcNow1});
            
            var endpoint2 = "127.0.0.1:5672";
            var utcNow2 = TimestampHelper.GetUtcNow().AddMinutes(1);
            _reconnectionProvider
                .AddReconnectingPeer(endpoint2, new ReconnectingPeer {Endpoint = endpoint2, NextAttempt = utcNow2});
            
            _reconnectionProvider.GetPeersReadyForReconnection(null).Count.ShouldBe(2);
            _reconnectionProvider.GetPeersReadyForReconnection(utcNow1.AddSeconds(1)).Count.ShouldBe(1);
            _reconnectionProvider.GetPeersReadyForReconnection(utcNow2.AddSeconds(1)).Count.ShouldBe(2);
        }

        [Fact]
        public void RemoveReconnectionPeer_Test()
        {
            var endpoint = "127.0.0.1:5677";
            var utcNow = TimestampHelper.GetUtcNow();

            _reconnectionProvider.AddReconnectingPeer(endpoint, new ReconnectingPeer { Endpoint = endpoint, NextAttempt = utcNow });
            _reconnectionProvider.RemoveReconnectionPeer(endpoint).ShouldBeTrue();

            _reconnectionProvider.GetReconnectingPeer(endpoint).ShouldBeNull();
            _reconnectionProvider.GetPeersReadyForReconnection(utcNow.AddSeconds(1)).Count.ShouldBe(0);
            
            _reconnectionProvider.RemoveReconnectionPeer(endpoint).ShouldBeFalse();
        }
    }
}