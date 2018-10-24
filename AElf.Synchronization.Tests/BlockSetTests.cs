using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using AElf.Synchronization.BlockSynchronization;
using Moq;
using Xunit;

namespace AElf.Synchronization.Tests
{
    public class BlockSetTests
    {
        [Fact]
        public void NormalTest()
        {
            var blockSet = new BlockSet();

            var blocks = MockSeveralBlocks(5);

            foreach (var block in blocks)
            {
                blockSet.AddBlock(block);
            }

            Assert.True(blockSet.InvalidBlockCount == 5);
            
            blockSet.RemoveExecutedBlock(blocks[0].BlockHashToHex);
            
            Assert.True(blockSet.ExecutedBlockCount == 1);
            Assert.True(blockSet.InvalidBlockCount == 4);
            
            blockSet.RemoveExecutedBlock(blocks[0].BlockHashToHex);

            Assert.True(blockSet.ExecutedBlockCount == 1);
            Assert.True(blockSet.InvalidBlockCount == 4);
            
            blockSet.RemoveExecutedBlock(blocks[1].BlockHashToHex);

            Assert.True(blockSet.ExecutedBlockCount == 2);
            Assert.True(blockSet.InvalidBlockCount == 3);
            
            Assert.True(blockSet.IsBlockReceived(Hash.LoadHex(blocks[2].BlockHashToHex), blocks[2].Index));
            Assert.False(blockSet.IsBlockReceived(Hash.Generate(), blocks[2].Index));
        }

        [Fact]
        public void FindLongestChainTest()
        {
            var blockSet = new BlockSet();

            var blocks = MockSeveralBlocks(5, 11);

            foreach (var block in blocks)
            {
                blockSet.AddBlock(block);
            }

            var forkHeight = blockSet.AnyLongerValidChain(12);
            
            Assert.True(forkHeight == 11);
        }

        private List<IBlock> MockSeveralBlocks(int number, int firstIndex = 0)
        {
            var list = new List<IBlock>();
            Hash temp = null;
            for (var i = firstIndex; i < number + firstIndex; i++)
            {
                var hash = Hash.Generate();
                list.Add(MockBlock((ulong) i, hash.DumpHex(), temp == null ? Hash.Generate() : temp));
                temp = hash;
            }

            return list;
        }

        private IBlock MockBlock(ulong index, string hashToHex, Hash preBlockHash)
        {
            return new Mock<IBlock>()
                .SetupProperty(b => b.Index, index)
                .SetupProperty(b => b.BlockHashToHex, hashToHex)
                .SetupProperty(b => b.Header, MockBlockHeader(preBlockHash))
                .Object;
        }
        
        private BlockHeader MockBlockHeader(Hash preBlockHash)
        {
            return new BlockHeader
            {
                MerkleTreeRootOfTransactions = Hash.Generate(),
                SideChainTransactionsRoot = Hash.Generate(),
                SideChainBlockHeadersRoot = Hash.Generate(),
                ChainId = Hash.Generate(),
                PreviousBlockHash = preBlockHash,
                MerkleTreeRootOfWorldState = Hash.Generate()
            };
        }
    }
}