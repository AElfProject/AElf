using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Protocol;
using AElf.Kernel.Node.Protocol.Exceptions;
using Castle.DynamicProxy.Generators;
using Moq;
using Xunit;

namespace AElf.Kernel.Tests.BlockSyncTests
{
    public class BlockSyncTests_AddBlockToSync
    {
        public static byte[] RandomFill(int count)
        {
            Random rnd = new Random();
            byte[] random = new byte[count];
            
            rnd.NextBytes(random);

            return random;
        }

        [Fact]
        public async Task AddBlockToSync_NullBlock_ShouldThrow()
        {
            BlockSynchronizer s = new BlockSynchronizer(null);
            
            Exception ex = await Assert.ThrowsAsync<InvalidBlockException>(() => s.AddBlockToSync(null));
            Assert.Equal("The block, blockheader or body is null", ex.Message);
            
            Exception ex2 = await Assert.ThrowsAsync<InvalidBlockException>(() => s.AddBlockToSync(new Block()));
            Assert.Equal("The block, blockheader or body is null", ex2.Message);
            
            Exception ex3 = await Assert.ThrowsAsync<InvalidBlockException>(() => s.AddBlockToSync(new Block()));
            Assert.Equal("The block, blockheader or body is null", ex3.Message);
        }

        [Fact]
        public async Task AddBlockToSync_NoTransactions_ShouldThrow()
        {
            BlockSynchronizer s = new BlockSynchronizer(null);
            
            Block b = new Block();
            b.Body = new BlockBody();
            b.Header = new BlockHeader();
            
            Exception ex = await Assert.ThrowsAsync<InvalidBlockException>(() => s.AddBlockToSync(b));
            Assert.Equal("The block contains no transactions", ex.Message);
        }

        [Fact]
        public async Task AddBlockToSync_NoHash_ShouldThrow()
        {
            BlockSynchronizer s = new BlockSynchronizer(null);
            
            Block b = new Block();
            b.Body = new BlockBody();
            b.Header = new BlockHeader();
            b.AddTransaction(new Hash());
            
            Exception ex = await Assert.ThrowsAsync<InvalidBlockException>(() => s.AddBlockToSync(b));
            Assert.Equal("Invalid block hash", ex.Message);
        }

        [Fact]
        public async Task AddBlockToSync_AllTxInPool_ShouldFireBlockSynched()
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
        
        /*[Fact]
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
        }*/
    }
}