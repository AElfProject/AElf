using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Network.Data;
using AElf.Network.Eventing;
using AElf.Network.Peers;
using AElf.Node.AElfChain;
using AElf.Node.Protocol;
using Easy.MessageHub;
using Moq;
using Xunit;

namespace AElf.Network.Tests.NetworkManagerTests
{
    public class NetworkManagerSyncTests
    {
        private const int GenesisHeight = 1;

        private Mock<IPeer> CreateMockPeer(int height = GenesisHeight)
        {
            Mock<IPeer> peer = new Mock<IPeer>();
            
            peer.Setup(m => m.KnownHeight).Returns(height);
            peer.Setup(m => m.AnyStashed).Returns(false);
            
            return peer;
        }
        
        private void SetHasAnnounce(Mock<IPeer> mock)
        {
            mock.Setup(m => m.AnyStashed).Returns(true);
            mock.Setup(m => m.SyncNextAnnouncement()).Returns(true);
        }

        #region Add Peer

        [Fact]
        public async Task OnPeerAdded_PeerLowerOrEqual_NoSync()
        {
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
            
            Mock<IPeer> peer = CreateMockPeer(); // Peer at height 1
            
            //Chain at height 1
            Mock<INodeService> chainService = new Mock<INodeService>();
            chainService.Setup(cs => cs.GetCurrentBlockHeightAsync()).ReturnsAsync(1);
            
            NetworkManager nm = new NetworkManager(peerManager.Object, null, chainService.Object, null);
            await nm.Start();
            
            // register peer 
            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(peer.Object, PeerEventType.Added));
            
            Assert.Null(nm.CurrentSyncSource); 
        }
        
        [Fact(Skip = "ToDebug")]
        public async Task OnPeerAdded_PeerHigher_Sync()
        {
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
            
            // Peer at height 2
            Mock<IPeer> firstPeer = CreateMockPeer(2);
            
            //Chain at height 1
            Mock<INodeService> chainService = new Mock<INodeService>();
            chainService.Setup(cs => cs.GetCurrentBlockHeightAsync()).ReturnsAsync(1);
            
            NetworkManager nm = new NetworkManager(peerManager.Object, null, chainService.Object, null);
            await nm.Start();

            bool syncStateTrueWasFired = false;
            bool syncStateValue = false;
            MessageHub.Instance.Subscribe<SyncStateChanged>(syncChanged =>
            {
                syncStateTrueWasFired = true;
                syncStateValue = syncChanged.IsSyncing;
            });
            
            // register peer 
            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(firstPeer.Object, PeerEventType.Added));
            
            // todo temp fix because the event is run on another thread 
            // todo probably not a good test, but for now no choice. 
            await Task.Delay(1000);
            
            Assert.Equal(firstPeer.Object, nm.CurrentSyncSource);
            Assert.True(syncStateTrueWasFired);
            Assert.True(syncStateValue);
        }
        
        [Fact]
        public async Task OnPeerAdded_AlreadySyncing_NoSync()
        {
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
            
            Mock<IPeer> firstPeer = CreateMockPeer(2); // Peer at height 1 - sync trigger
            Mock<IPeer> scdPeer = CreateMockPeer(1); // Peer at height 2 
            
            // Chain at height 1
            Mock<INodeService> chainService = new Mock<INodeService>();
            chainService.Setup(cs => cs.GetCurrentBlockHeightAsync()).ReturnsAsync(1);
            
            // Start manager
            NetworkManager nm = new NetworkManager(peerManager.Object, null, chainService.Object, null);
            await nm.Start();
            
            // register peer that will trigger the sync 
            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(firstPeer.Object, PeerEventType.Added));
            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(scdPeer.Object, PeerEventType.Added));
            
            Assert.Equal(firstPeer.Object, nm.CurrentSyncSource);
        }

        #endregion

        #region Disconnect

        /* Tests that when a node fails to provide the block that he announced */
        /* this peer will be terminated and current sync peer update if needed */
        
        [Fact]
        public async Task OnPeerDisconnect_UpdateCurrentSyncSource()
        {
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();

            Mock<IPeer> firstPeer = CreateMockPeer(2);
            
            Mock<INodeService> chainService = new Mock<INodeService>();
            chainService.Setup(cs => cs.GetCurrentBlockHeightAsync()).ReturnsAsync(1);
            
            // Start manager
            NetworkManager nm = new NetworkManager(peerManager.Object, null, chainService.Object, null);
            await nm.Start();
            
            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(firstPeer.Object, PeerEventType.Added));
            firstPeer.Raise(m => m.PeerDisconnected += null, new PeerDisconnectedArgs { Peer = firstPeer.Object, Reason = DisconnectReason.BlockRequestTimeout});
            
            Assert.Null(nm.CurrentSyncSource);
        }
        
        [Fact]
        public async Task OnPeerDisconnect_SwitchSyncSource()
        {
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();

            Mock<IPeer> firstPeer = CreateMockPeer(2);
            Mock<IPeer> secondPeer = CreateMockPeer(2);

            SetHasAnnounce(secondPeer);
            
            Mock<INodeService> chainService = new Mock<INodeService>();
            chainService.Setup(cs => cs.GetCurrentBlockHeightAsync()).ReturnsAsync(1);
            
            // Start manager
            NetworkManager nm = new NetworkManager(peerManager.Object, null, chainService.Object, null);
            await nm.Start();
            
            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(firstPeer.Object, PeerEventType.Added));
            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(secondPeer.Object, PeerEventType.Added));

            firstPeer.Raise(m => m.PeerDisconnected += null, new PeerDisconnectedArgs { Peer = firstPeer.Object, Reason = DisconnectReason.BlockRequestTimeout});
            
            Assert.NotNull(nm.CurrentSyncSource);
            Assert.Equal(secondPeer.Object, nm.CurrentSyncSource);
        }
               
        #endregion
    }
} 