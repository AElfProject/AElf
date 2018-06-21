using System.Threading.Tasks;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Protocol;
using AElf.Network.Data;
using AElf.Network.Peers;
using Moq;
using Xunit;

namespace AElf.Kernel.Tests.BlockSyncTests
{
    public class BlockSyncHelpers_OnePeerDownload
    {
        
        /* This simulates downloading the blockchain from one peer that already
         * has multiple block : from heights 0 through H. We start downloading
         * from height 0. The target will be height H. 
         */
        
        [Fact]
        public async Task OnePeerSynchronizeBlock_Sequential()
        {
            int distantPeerHeight = 10;
            int currentHeight = 1;
            
            // Fake node 
            Mock<IAElfNode> mockNode = new Mock<IAElfNode>();
            IAElfNode node = mockNode.Object;
            
            // The peer that we're going to download from
            Mock<IPeer> mockPeer = new Mock<IPeer>();
            IPeer peer = mockPeer.Object;
            
            // Setup peer manager - to get the unique peer
            Mock<IPeerManager> mockPeerManager = new Mock<IPeerManager>();
            
            BlockSynchronizer synchronizer = new BlockSynchronizer(node, mockPeerManager.Object);
            synchronizer.SetNodeHeight(currentHeight);
            
            // First, the BlockSynchronizer should have no peer height info
            synchronizer.DoCycle(null);
            
            // Verify that the height as been queried 
            mockPeerManager.Verify(p => p.BroadcastMessage(
                    It.Is<MessageTypes>(i => i == MessageTypes.HeightRequest), 
                    It.IsAny<byte[]>(),
                    It.IsAny<int>()), 
                Times.Exactly(1));
            
            // We simulate that the node has received a response to the request
            synchronizer.SetPeerHeight(peer, distantPeerHeight);
            
            byte[] sendRequest = null;
            mockPeer.Setup(p => p.SendAsync(It.IsAny<byte[]>())).Callback<byte[]>(b => sendRequest = b);
            
            synchronizer.DoCycle(null);
            
            mockPeer.Verify(p => p.SendAsync(It.IsAny<byte[]>()), Times.Exactly(1));
            AElfPacketData pd = AElfPacketData.Parser.ParseFrom(sendRequest);
            BlockRequest req = BlockRequest.Parser.ParseFrom(pd.Payload);
            
            Assert.NotNull(req);
            Assert.Equal(synchronizer.CurrentHeight, req.Height);

            //Block blockToAdd = BlockSyncHelpers.GenerateValidBlockToSync((ulong)synchronizer.CurrentHeight);
            //await synchronizer.AddBlockToSync(blockToAdd);
        }
    }
}