using AElf.ChainController.EventMessages;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Network.Eventing;
using AElf.Network.Peers;
using AElf.Node.Protocol;
using Easy.MessageHub;
using Moq;
using Xunit;

namespace AElf.Network.Tests.NetworkManagerTests
{
    public class ForkSyncTests
    {
        [Fact]
        public void UnlinkableBlockEvt_ShouldTrigger_Request()
        {
            int unlinkableHeaderIndex = 1;
            int requestCount = NetworkManager.DefaultHeaderRequestCount;
            
            // Peer at height 2
            Mock<IPeer> firstPeer = new Mock<IPeer>();
            firstPeer.Setup(m => m.RequestHeaders(It.IsAny<int>(), It.IsAny<int>()));
            firstPeer.Setup(m => m.KnownHeight).Returns(2);
            
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
            ChainConfig.Instance.ChainId = "";
            
            NetworkManager nm = new NetworkManager(peerManager.Object, null, null, null, null);
            
            // register peer 
            peerManager.Raise(m => m.PeerEvent += null, new PeerEventArgs(firstPeer.Object, PeerEventType.Added));
            
            MessageHub.Instance.Publish(new UnlinkableHeader(new BlockHeader { Index = (ulong)unlinkableHeaderIndex }));
            
            // Verify that the peer is used to request the appropriate header
            firstPeer.Verify(mock => mock.RequestHeaders(
                It.Is<int>(hIndex => hIndex == unlinkableHeaderIndex), It.Is<int>(rCount => rCount == requestCount)), 
                Times.Once());
        }
    }
}