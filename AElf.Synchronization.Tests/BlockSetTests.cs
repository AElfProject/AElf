using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using AElf.Synchronization.BlockSynchronization;
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

//            Assert.True(blockSet.InvalidBlockCount == 5);
//
//            blockSet.RemoveExecutedBlockFromCache(blocks[0].BlockHashToHex);
//
//            Assert.True(blockSet.ExecutedBlockCount == 1);
//            Assert.True(blockSet.InvalidBlockCount == 4);
//
//            blockSet.RemoveExecutedBlockFromCache(blocks[0].BlockHashToHex);
//
//            Assert.True(blockSet.ExecutedBlockCount == 1);
//            Assert.True(blockSet.InvalidBlockCount == 4);
//
//            blockSet.RemoveExecutedBlockFromCache(blocks[1].BlockHashToHex);
//
//            Assert.True(blockSet.ExecutedBlockCount == 2);
//            Assert.True(blockSet.InvalidBlockCount == 3);
//
//            Assert.True(blockSet.IsBlockReceived(Hash.LoadHex(blocks[2].BlockHashToHex), blocks[2].Index));
//            Assert.False(blockSet.IsBlockReceived(Hash.Generate(), blocks[2].Index));
        }

        [Fact]
        public void FindFortHeightTest()
        {
            var blockSet = new BlockSet();

            // Generate block from 11 to 15
            var blocks = MockSeveralBlocks(5, 11);

            foreach (var block in blocks)
            {
                blockSet.AddBlock(block);
            }

            var forkHeight = blockSet.AnyLongerValidChain(14);

            Assert.True(forkHeight == 11);
        }

        private List<IBlock> MockSeveralBlocks(int number, int firstIndex = 0)
        {
            var list = new List<IBlock>();
            var temp = Hash.Generate();
            for (var i = firstIndex; i < number + firstIndex; i++)
            {
                var block = MockBlock((ulong) i, Hash.Generate().DumpHex(), temp == null ? Hash.Generate() : temp);
                list.Add(block);
                temp = block.GetHash();
            }

            return list;
        }

        private IBlock MockBlock(ulong index, string hashToHex, Hash preBlockHash)
        {
            return new Block
            {
                BlockHashToHex = hashToHex,
                Header = new BlockHeader
                {
                    MerkleTreeRootOfTransactions = Hash.Generate(),
                    SideChainTransactionsRoot = Hash.Generate(),
                    SideChainBlockHeadersRoot = Hash.Generate(),
                    ChainId = Hash.Generate(),
                    PreviousBlockHash = preBlockHash,
                    MerkleTreeRootOfWorldState = Hash.Generate(),
                    Index = index
                },
                Index = index
            };
        }
    }
}