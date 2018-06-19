using System;
using System.Collections.Generic;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Protocol;
using Moq;
using Xunit;

namespace AElf.Kernel.Tests.BlockSyncTests
{
    public class BlockSyncTests
    {
        public static byte[] RandomFill(int count)
        {
            Random rnd = new Random();
            byte[] random = new byte[count];
            
            rnd.NextBytes(random);

            return random;
        }
        
        [Fact]
        public void AddBlock_AllTxInPool_ShouldFireBlockSynched()
        {
            Mock<IAElfNode> mock = new Mock<IAElfNode>();
            mock.Setup(n => n.GetMissingTransactions(It.IsAny<IBlock>())).Returns(new List<Hash>());
            IAElfNode m = mock.Object;
            
            BlockSynchronizer s = new BlockSynchronizer(m);
            
            List<BlockSynchedArgs> receivedEvents = new List<BlockSynchedArgs>();
            
            s.BlockSynched += (sender, e) =>
            {
                BlockSynchedArgs args = e as BlockSynchedArgs;
                receivedEvents.Add(args);
            };
            
            Block b = new Block();
            s.AddBlockToSync(b);
            
            Assert.Equal(1, receivedEvents.Count);
            Assert.Equal(receivedEvents[0].Block, b);
        }
    }
}