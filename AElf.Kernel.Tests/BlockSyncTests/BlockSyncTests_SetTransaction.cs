using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Protocol;
using Moq;
using Xunit;

namespace AElf.Kernel.Tests.BlockSyncTests
{
    public class BlockSyncTests_SetTransaction
    {
        [Fact]
        public async Task AddBlockToSync_TxMissing_ShouldPutBlockToSync()
        {
            /*var missingTxHash = ByteArrayHelpers.RandomFill(256);
            var returnTxHashes = new List<Hash> { new Hash(missingTxHash) };
            
            Mock<IAElfNode> mock = new Mock<IAElfNode>();
            mock.Setup(n => n.GetMissingTransactions(It.IsAny<IBlock>())).Returns(returnTxHashes);
            
            IAElfNode m = mock.Object;
            
            BlockSynchronizer s = new BlockSynchronizer(m, null);

            Block b = BlockSyncHelpers.GenerateValidBlockToSync();
            b.AddTransaction(missingTxHash);
            
            await s.AddBlockToSync(b);
            s.SetTransaction(missingTxHash);

            byte[] array = b.GetHash().GetHashBytes();
            PendingBlock p = s.GetBlock(array);
            
            Assert.Equal(p.BlockHash, array);
            Assert.Equal(p.MissingTxs.Count, 0);*/
        }
    }
}