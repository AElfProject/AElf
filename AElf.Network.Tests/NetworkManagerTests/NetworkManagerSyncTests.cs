using System.Threading.Tasks;
using AElf.Configuration.Config.Chain;
using AElf.Network.Eventing;
using AElf.Network.Peers;
using AElf.Node.AElfChain;
using AElf.Node.Protocol;
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
            return peer;
        }

        #region Add Peer

        [Fact]
        public async Task OnPeerAdded_PeerLowerOrEqual_NoSync()
        {
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
            ChainConfig.Instance.ChainId = "";
            
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
        
        [Fact]
        public async Task OnPeerAdded_PeerHigher_Sync()
        {
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
            ChainConfig.Instance.ChainId = "";
            
            // Peer at height 2
            Mock<IPeer> firstPeer = CreateMockPeer(2);
            
            //Chain at height 1
            Mock<INodeService> chainService = new Mock<INodeService>();
            chainService.Setup(cs => cs.GetCurrentBlockHeightAsync()).ReturnsAsync(1);
            
            NetworkManager nm = new NetworkManager(peerManager.Object, null, chainService.Object, null);
            await nm.Start();
            
            // register peer 
            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(firstPeer.Object, PeerEventType.Added));
            
            Assert.Equal(firstPeer.Object, nm.CurrentSyncSource);
        }
        
        [Fact]
        public async Task OnPeerAdded_AlreadySyncing_NoSync()
        {
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
            ChainConfig.Instance.ChainId = "";
            
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
    }
}