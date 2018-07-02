using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Network.Config;
using AElf.Network.Data;
using AElf.Network.Peers;
using Moq;
using Xunit;

namespace AElf.Network.Tests.PeerManagement
{
    public class PeerMaintenanceUnitTests
    {
        [Fact(Skip = "todo")]
        public async Task DoPeerMaintenance_ShouldAddFirstReachableBootnode_IfNoPeers()
        {
            /*
            var bootnodes = new List<NodeData>();
            
            NodeData boot01 = NodeData.FromString("127.0.0.1:6001");
            bootnodes.Add(boot01);
            NodeData boot02 = NodeData.FromString("127.0.0.1:6002");
            bootnodes.Add(boot02);
            NodeData boot03 = NodeData.FromString("127.0.0.1:6003"); // First reachable
            bootnodes.Add(boot03);
            
            // Set up the node dialer so it is able to connect to the last bootnode
            var ndialer = new Mock<INodeDialer>();
            
            ndialer.Setup(m => m.DialAsync(boot01)).Returns(Task.FromResult<IPeer>(null));
            ndialer.Setup(m => m.DialAsync(boot02)).Returns(Task.FromResult<IPeer>(null));

            var mockPeer = CreateMockPeer(true);
            ndialer.Setup(m => m.DialAsync(boot03)).Returns(Task.FromResult<IPeer>(mockPeer));
            
            AElfNetworkConfig conf = new AElfNetworkConfig(); // default conf
            conf.Bootnodes = bootnodes;
            
            PeerManager peerManager = new PeerManager(null, conf, ndialer.Object, null);
            
            peerManager.DoPeerMaintenance();

            IPeer p = peerManager.GetPeer(mockPeer);
            
            Assert.NotNull(p);
            */
        }

        [Fact(Skip = "todo")]
        public async Task DoPeerMaintenance_ShouldDropBootnode_AfterThreshold()
        {
            /*
            PeerManager peerManager = new PeerManager(null, null, null, null);

            for (int i = 0; i < peerManager.BootnodeDropThreshold; i++)
            {
                peerManager.AddPeer(CreateMockPeer());
            }

            var bootnode = CreateMockPeer(true);
            peerManager.AddPeer(bootnode);
            
            Assert.NotNull(peerManager.GetPeer(bootnode)); // maybe not necessary
            
            peerManager.DoPeerMaintenance();
            
            Assert.Null(peerManager.GetPeer(bootnode));
            */
        }

        private IPeer CreateMockPeer(bool isBootnode = false, bool canConnect = true)
        {
            Mock<IPeer> mock = new Mock<IPeer>();
            mock.Setup(m => m.IsBootnode).Returns(isBootnode);
            mock.Setup(m => m.StartListeningAsync()).Returns(Task.FromResult(true));
            mock.Setup(m => m.DoConnectAsync()).Returns(Task.FromResult(canConnect));

            return mock.Object;
        }

        [Fact(Skip = "todo")]
        public void AddPeer_ShouldReturnTrue_NotBootnode_NotInList()
        {
            PeerManager peerManager = new PeerManager(null, null, null, null);
            IPeer peer = CreateMockPeer();

            Assert.True(peerManager.AddPeer(peer));
        }
        
        [Fact(Skip = "todo")]
        public void AddPeer_ShouldReturnFalse_NotBootnode_InList()
        {
            PeerManager peerManager = new PeerManager(null, null, null, null);
            IPeer peer = CreateMockPeer();

            peerManager.AddPeer(peer);

            Assert.False(peerManager.AddPeer(peer));
        }
        
        [Fact(Skip = "todo")]
        public void AddPeer_ShouldReturnFalse_Bootnode_NotInList()
        {/*
            PeerManager peerManager = new PeerManager(null, null, null, null);
            IPeer peer = CreateMockPeer(true);

            Assert.False(peerManager.AddPeer(peer));
            */
        }
        
        [Fact(Skip = "todo")]
        public void AddPeer_ShouldReturnFalse_Bootnode_InList()
        {
            PeerManager peerManager = new PeerManager(null, null, null, null);
            IPeer peer = CreateMockPeer(true);
            peerManager.AddPeer(peer);

            Assert.False(peerManager.AddPeer(peer));
        }
        
        [Fact]
        public void AddPeer_ShouldReturnFalse_NullParam()
        {
            PeerManager peerManager = new PeerManager(null, null, null, null);
            IPeer peer = null;

            Assert.False(peerManager.AddPeer(peer));
        }
    }
}