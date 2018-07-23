using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Protocol;
using AElf.Network.Data;
using AElf.Network.Data.Protobuf;
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
        
        [Fact(Skip = "Moq.MockException")]
        public async Task OnePeerSynchronizeBlock_Sequential()
        {
            int distantPeerHeight = 2;
            int currentHeight = 0;
            
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
                    It.Is<MessageType>(i => i == MessageType.HeightRequest), 
                    It.IsAny<byte[]>(),
                    It.IsAny<int>()), 
                Times.Exactly(1));
            
            /*** Cycle 1 - Send height requests ***/
            
            // We simulate that the node has received a response to the request
            synchronizer.SetPeerHeight(peer, distantPeerHeight);
            
            List<byte[]> sendRequests = new List<byte[]>();
            mockPeer.Setup(p => p.EnqueueOutgoing(It.IsAny<byte[]>())).Callback<byte[]>(b => sendRequests.Add(b));
            
            synchronizer.DoCycle(null);
            
            mockPeer.Verify(p => p.EnqueueOutgoing(It.IsAny<byte[]>()), Times.Exactly(1));
            AElfPacketData pd = AElfPacketData.Parser.ParseFrom(sendRequests[0]);
            BlockRequest req = BlockRequest.Parser.ParseFrom(pd.Payload);
            
            Assert.NotNull(req);
            //Assert.Equal(synchronizer.CurrentHeight, req.Height);

            /*** Cycle 2 - Add block + cycle (request next block) ***/
            
            FakeChain f = new FakeChain(3);
            f.Generate();
            
            // Setup the node so there's never missing transactions
            mockNode.Setup(n => n.GetMissingTransactions(It.IsAny<IBlock>()))
                .Returns(new List<Hash>());
            
            // Block are always succesfully executed
            mockNode.Setup(n => n.ExecuteAndAddBlock(It.IsAny<IBlock>()))
                .Returns(Task.FromResult(new BlockExecutionResult(true, ValidationError.Success)));

            mockPeer.ResetCalls();
            
            Block blockToAdd = f.GetAtHeight(0);
            //await synchronizer.AddBlockToSync(blockToAdd);
            
            synchronizer.DoCycle(null);
            
            mockPeer.Verify(p => p.EnqueueOutgoing(It.IsAny<byte[]>()), Times.Exactly(1));
            AElfPacketData pd2 = AElfPacketData.Parser.ParseFrom(sendRequests[1]);
            BlockRequest req2 = BlockRequest.Parser.ParseFrom(pd2.Payload);
            
            Assert.NotNull(req2);
            //Assert.Equal(synchronizer.CurrentHeight, req2.Height);
        }

        [Fact(Skip = "todo")]
        public void TestChain()
        {
            /*
            FakeChain f = new FakeChain(3);
            f.Generate();

            Block b1 = f.GetAtHeight(0);
            Block b2 = f.GetAtHeight(1);
            Block b3 = f.GetAtHeight(2);

            List<Transaction> txsB1 = f.GetBlockTransactions(b1);
            ;
            */
        }
    }
}