using AElf.ChainController.EventMessages;
using AElf.Configuration;
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
        public void EventTest()
        {
            Mock<IPeerManager> peerManager = new Mock<IPeerManager>();
            
            NodeConfig.Instance.ChainId = "";
            NetworkManager nm = new NetworkManager(peerManager.Object, null, null, null);
            
            MessageHub.Instance.Publish(new ChainInitialized(null));
        }
    }
}