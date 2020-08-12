using System;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol.Types;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.OS.Worker
{
    public class PeerReconnectionWorkerTests : PeerReconnectionTestBase
    {
        private readonly PeerReconnectionWorker _peerReconnectionWorker;
        private readonly IPeerReconnectionStateProvider _peerReconnectionStateProvider;
        private readonly IPeerPool _peerPool;
        private readonly NetworkOptions _networkOptions;
        
        public PeerReconnectionWorkerTests()
        {
            _peerReconnectionWorker = GetRequiredService<PeerReconnectionWorker>();
            _peerReconnectionStateProvider = GetRequiredService<IPeerReconnectionStateProvider>();
            _peerPool = GetRequiredService<IPeerPool>();
            _networkOptions = GetRequiredService<IOptionsSnapshot<NetworkOptions>>().Value;
        }

        [Fact]
        public async Task DoReconnectionJob_NoReconnection_Test()
        {
            var peer1 = CreatePeer("Peer1", "127.0.0.1:8001",true);
            _peerPool.TryAddPeer(peer1);
            var peer2 = CreatePeer("Peer2", "127.0.0.1:8002",false);
            _peerPool.TryAddPeer(peer2);

            await _peerReconnectionWorker.DoReconnectionJobAsync();

            _peerPool.FindPeerByPublicKey(peer1.Info.Pubkey).ShouldBeNull();
            _peerPool.FindPeerByPublicKey(peer2.Info.Pubkey).ShouldNotBeNull();
        }
        
        [Fact]
        public async Task DoReconnectionJob_InvalidEndpoint_Test()
        {
            var invalidEndpoint = "127.0.0.1:abc";
            _peerReconnectionStateProvider.AddReconnectingPeer(invalidEndpoint, new ReconnectingPeer
            {
                Endpoint = invalidEndpoint,
                RetryCount = 1,
                NextAttempt = TimestampHelper.GetUtcNow().AddMinutes(-10),
                DisconnectionTime = TimestampHelper.GetUtcNow()
            });

            await _peerReconnectionWorker.DoReconnectionJobAsync();

            _peerReconnectionStateProvider.GetReconnectingPeer(invalidEndpoint).ShouldBeNull();
        }
        
        [Fact]
        public async Task DoReconnectionJob_AlreadyInPeerPool_Test()
        {
            var endpoint = "127.0.0.1:123";
            var peer = CreatePeer("pubkey", endpoint);
            _peerPool.TryAddPeer(peer);
            
            _peerReconnectionStateProvider.AddReconnectingPeer(endpoint, new ReconnectingPeer
            {
                Endpoint = endpoint,
                RetryCount = 1,
                NextAttempt = TimestampHelper.GetUtcNow().AddMinutes(-10),
                DisconnectionTime = TimestampHelper.GetUtcNow()
            });

            await _peerReconnectionWorker.DoReconnectionJobAsync();

            _peerReconnectionStateProvider.GetReconnectingPeer(endpoint).ShouldBeNull();
        }
        
        [Fact]
        public async Task DoReconnectionJob_ReconnectSuccess_Test()
        {
            var endpoint = "127.0.0.1:8001";

            _peerReconnectionStateProvider.AddReconnectingPeer(endpoint, new ReconnectingPeer
            {
                Endpoint = endpoint,
                RetryCount = 1,
                NextAttempt = TimestampHelper.GetUtcNow().AddMinutes(-10),
                DisconnectionTime = TimestampHelper.GetUtcNow()
            });

            await _peerReconnectionWorker.DoReconnectionJobAsync();

            _peerReconnectionStateProvider.GetReconnectingPeer(endpoint).ShouldBeNull();
        }
        
        [Fact]
        public async Task DoReconnectionJob_ReconnectFailed_Test()
        {
            var endpoint = "127.0.0.1:8002";

            var now = TimestampHelper.GetUtcNow();
            var nextAttemptTime = TimestampHelper.GetUtcNow().AddMinutes(-10);
            _peerReconnectionStateProvider.AddReconnectingPeer(endpoint, new ReconnectingPeer
            {
                Endpoint = endpoint,
                RetryCount = 1,
                NextAttempt = nextAttemptTime,
                DisconnectionTime = TimestampHelper.GetUtcNow()
            });

            await _peerReconnectionWorker.DoReconnectionJobAsync();

            var reconnection = _peerReconnectionStateProvider.GetReconnectingPeer(endpoint);
            
            var timeExtension = _networkOptions.PeerReconnectionPeriod * (int)Math.Pow(2, reconnection.RetryCount);
            reconnection.NextAttempt.ShouldBeGreaterThan(now.AddMilliseconds(timeExtension));
        }
        
        [Fact]
        public async Task DoReconnectionJob_ReconnectFailedAndExceedMaxTime_Test()
        {
            var endpoint = "127.0.0.1:8002";

            var nextAttemptTime = TimestampHelper.GetUtcNow().AddMinutes(-10);
            _peerReconnectionStateProvider.AddReconnectingPeer(endpoint, new ReconnectingPeer
            {
                Endpoint = endpoint,
                RetryCount = 10,
                NextAttempt = nextAttemptTime,
                DisconnectionTime = TimestampHelper.GetUtcNow().AddDays(-1)
            });

            await _peerReconnectionWorker.DoReconnectionJobAsync();

            _peerReconnectionStateProvider.GetReconnectingPeer(endpoint).ShouldBeNull();
        }

        private IPeer CreatePeer(string pubkey, string endpoint, bool isInvalid = false)
        {
            AElfPeerEndpointHelper.TryParse(endpoint, out var aelfEndpoint);
            var peer = new Mock<IPeer>();
            peer.Setup(p => p.IsInvalid).Returns(isInvalid);
            peer.Setup(p => p.Info).Returns(new PeerConnectionInfo
            {
                Pubkey = pubkey,
            });
            peer.Setup(p => p.RemoteEndpoint).Returns(aelfEndpoint);
            return peer.Object;
        }
    }
}