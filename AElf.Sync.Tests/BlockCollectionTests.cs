using System;
using System.Collections.Generic;
using System.Linq;
using AElf.ChainController;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Network;
using AElf.Node;
using AElf.Node.Protocol;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;

namespace AElf.Sync.Tests
{
    /// <summary>
    /// Test the functionality of BlockCollection
    /// </summary>
    [UseAutofacTestFramework]
    public class BlockCollectionTests
    {
        private readonly IChainService _chainService;

        public BlockCollectionTests(IChainService chainService)
        {
            _chainService = chainService;
        }

        [Fact]
        public void AddPendingBlock_Initial()
        {
            NodeConfig.Instance.ChainId = Hash.Generate().DumpHex();
            var blockCollection = new BlockCollection(_chainService);

            // Initial sync.
            var pendingBlock1 = GeneratePendingBlock(1, Hash.Genesis, AElfProtocolMsgType.Block);
            var pendingBlock2 = GeneratePendingBlock(2, pendingBlock1.Block.GetHash(), AElfProtocolMsgType.Block);
            var pendingBlock3 = GeneratePendingBlock(3, pendingBlock2.Block.GetHash());

            blockCollection.AddPendingBlock(pendingBlock3);
            Assert.Equal(1, blockCollection.Count);
            Assert.False(blockCollection.ReceivedAllTheBlocksBeforeTargetBlock);

            blockCollection.AddPendingBlock(pendingBlock1);
            Assert.False(blockCollection.ReceivedAllTheBlocksBeforeTargetBlock);

            blockCollection.AddPendingBlock(pendingBlock2);
            Assert.Equal(3, blockCollection.Count);
            Assert.True(blockCollection.ReceivedAllTheBlocksBeforeTargetBlock);
        }

        [Fact]
        public void AddPendingBlock_Initial_Reverse()
        {
            NodeConfig.Instance.ChainId = Hash.Generate().DumpHex();
            var blockCollection = new BlockCollection(_chainService);

            // Initial sync.
            var pendingBlock1 = GeneratePendingBlock(1, Hash.Genesis, AElfProtocolMsgType.Block);
            var pendingBlock2 = GeneratePendingBlock(2, pendingBlock1.Block.GetHash(), AElfProtocolMsgType.Block);
            var pendingBlock3 = GeneratePendingBlock(3, pendingBlock2.Block.GetHash());

            blockCollection.AddPendingBlock(pendingBlock3);
            Assert.Equal(1, blockCollection.Count);
            Assert.False(blockCollection.ReceivedAllTheBlocksBeforeTargetBlock);

            // Not in order.
            blockCollection.AddPendingBlock(pendingBlock2);
            Assert.False(blockCollection.ReceivedAllTheBlocksBeforeTargetBlock);

            blockCollection.AddPendingBlock(pendingBlock1);
            Assert.Equal(3, blockCollection.Count);
            Assert.True(blockCollection.ReceivedAllTheBlocksBeforeTargetBlock);
        }

        /*/// <summary>
        /// Already contains the removing pending block test.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public Tuple<BlockCollection, Hash> InitialSync()
        {
            var blockCollection = new BlockCollection(_chainService);

            // Initial sync.
            var pendingBlock1 = GeneratePendingBlock(1, Hash.Genesis, AElfProtocolMsgType.Block);
            var pendingBlock2 = GeneratePendingBlock(2, pendingBlock1.Block.GetHash(), AElfProtocolMsgType.Block);
            var pendingBlock3 = GeneratePendingBlock(3, pendingBlock2.Block.GetHash());
            blockCollection.AddPendingBlock(pendingBlock3);
            blockCollection.AddPendingBlock(pendingBlock1);
            blockCollection.AddPendingBlock(pendingBlock2);
            blockCollection.RemovePendingBlock(pendingBlock1);
            blockCollection.RemovePendingBlock(pendingBlock2);
            blockCollection.RemovePendingBlock(pendingBlock3);

            Assert.Equal(3, blockCollection.Count);
            Assert.Equal(1, blockCollection.BranchedChainsCount);

            return new Tuple<BlockCollection, Hash>(blockCollection, pendingBlock3.Block.GetHash());
        }

        [Fact]
        public void AddPendingBlock_SameBlock()
        {
            var initial = InitialSync();
            var blockCollection = initial.Item1;
            var targetBlockHash = initial.Item2;

            var pendingBlock = GeneratePendingBlock(4, targetBlockHash);

            blockCollection.AddPendingBlock(pendingBlock);
            // Should just ignore.
            blockCollection.AddPendingBlock(pendingBlock);

            Assert.Equal(4, blockCollection.Count);
            Assert.Equal(1, blockCollection.BranchedChainsCount);
        }

        [Fact]
        public void AddPendingBlock_SameHeight()
        {
            var initial = InitialSync();
            var blockCollection = initial.Item1;
            var targetBlockHash = initial.Item2;

            var pendingBlock1 = GeneratePendingBlock(4, targetBlockHash);
            var pendingBlock2 = GeneratePendingBlock(4, targetBlockHash);

            blockCollection.AddPendingBlock(pendingBlock1);
            blockCollection.AddPendingBlock(pendingBlock2);
            Assert.Equal(1, blockCollection.Count);
            Assert.Equal(1, blockCollection.BranchedChainsCount);

            var pendingBlock3 = GeneratePendingBlock(4, targetBlockHash);
            blockCollection.AddPendingBlock(pendingBlock3);

            Assert.Equal(1, blockCollection.Count);
            Assert.Equal(2, blockCollection.BranchedChainsCount);

            var pendingBlock4 = GeneratePendingBlock(5, pendingBlock1.Block.GetHash());
            var pendingBlock5 = GeneratePendingBlock(5, pendingBlock1.Block.GetHash());

            blockCollection.AddPendingBlock(pendingBlock4);
            blockCollection.AddPendingBlock(pendingBlock5);

            Assert.Equal(2, blockCollection.Count);
            Assert.Equal(3, blockCollection.BranchedChainsCount);
        }

        [Fact]
        public void CheckoutBranchedChainTest()
        {
            var initial = InitialSync();
            var blockCollection = initial.Item1;
            var targetBlockHash = initial.Item2;

            var localBlocks = GeneratePendingBlocks(4, 14, targetBlockHash);
            var branchedBlocks = GeneratePendingBlocks(12, 16, localBlocks[7].Block.Header.PreviousBlockHash);

            foreach (var pendingBlock in localBlocks)
            {
                blockCollection.AddPendingBlock(pendingBlock);
            }

            Assert.Equal((ulong) 14, blockCollection.PendingBlockHeight);

            var startForkBlock = branchedBlocks.First();
            blockCollection.AddPendingBlock(startForkBlock);

            Assert.Equal((ulong) 14, blockCollection.PendingBlockHeight);
            Assert.Equal(1, blockCollection.BranchedChainsCount);

            foreach (var branchedBlock in branchedBlocks.Skip(1))
            {
                blockCollection.AddPendingBlock(branchedBlock);
            }

            Assert.Equal((ulong) 16, blockCollection.PendingBlockHeight);
            // The pending blocks list has already switched to longest branched chain.
            Assert.Equal(1, blockCollection.BranchedChainsCount);
        }

        [Fact]
        public void BranchedChainTest()
        {
            var initial = InitialSync();
            var blockCollection = initial.Item1;
            var targetBlockHash = initial.Item2;

            var pendingBlock1 = GeneratePendingBlock(4, targetBlockHash);
            blockCollection.AddPendingBlock(pendingBlock1);

            // [Current State]
            // PendingBlocks: 4
            // BranchedChains:
            Assert.Equal(1, blockCollection.Count);
            Assert.Equal("4", blockCollection.PendingBlockHeight.ToString());
            Assert.Equal(0, blockCollection.BranchedChainsCount);

            var pendingBlock2 = GeneratePendingBlock(5, pendingBlock1.Block.GetHash());
            blockCollection.AddPendingBlock(pendingBlock2);

            // [Current State]
            // PendingBlocks: 4 - 5
            // BranchedChains:
            Assert.Equal(2, blockCollection.Count);
            Assert.Equal("5", blockCollection.PendingBlockHeight.ToString());
            Assert.Equal(0, blockCollection.BranchedChainsCount);

            var branchedPendingBlock2 = GeneratePendingBlock(5, pendingBlock1.Block.GetHash());
            blockCollection.AddPendingBlock(branchedPendingBlock2);

            // [Current State]
            // PendingBlocks: 4 - 5
            // BranchedChains:
            // 5'
            Assert.Equal(2, blockCollection.Count);
            Assert.Equal("5", blockCollection.PendingBlockHeight.ToString());
            Assert.Equal(1, blockCollection.BranchedChainsCount);

            var pendingBlock3 = GeneratePendingBlock(6, pendingBlock2.Block.GetHash());
            blockCollection.AddPendingBlock(pendingBlock3);

            // [Current State]
            // PendingBlocks: 4 - 5 - 6
            // BranchedChains:
            // 5'
            Assert.Equal(3, blockCollection.Count);
            Assert.Equal("6", blockCollection.PendingBlockHeight.ToString());
            Assert.Equal(1, blockCollection.BranchedChainsCount);

            var branchedBlock3 = GeneratePendingBlock(6, branchedPendingBlock2.Block.GetHash());
            blockCollection.AddPendingBlock(branchedBlock3);

            // [Current State]
            // PendingBlocks: 4 - 5 - 6
            // BranchedChains:
            // 5' - 6'
            Assert.Equal(3, blockCollection.Count);
            Assert.Equal("6", blockCollection.PendingBlockHeight.ToString());
            Assert.Equal(1, blockCollection.BranchedChainsCount);
        }*/

        private List<PendingBlock> GeneratePendingBlocks(ulong startIndex, ulong endIndex, Hash preBlockHash = null)
        {
            var list = new List<PendingBlock>();
            if (preBlockHash != null)
            {
                var block = GeneratePendingBlock(startIndex, preBlockHash);
                preBlockHash = block.Block.GetHash();
                list.Add(block);
                startIndex++;
            }

            for (var i = startIndex; i <= endIndex; i++)
            {
                var block = GeneratePendingBlock(i, preBlockHash);
                list.Add(block);
                preBlockHash = block.Block.GetHash();
            }

            return list;
        }

        private static PendingBlock GeneratePendingBlock(ulong index, Hash preBlockHash = null,
            AElfProtocolMsgType msgType = AElfProtocolMsgType.NewBlock)
        {
            var block = GenerateBlock(Hash.Generate(), preBlockHash != null ? preBlockHash : Hash.Generate(), index);
            return new PendingBlock(block.GetHash().ToByteArray(), block, null, msgType);
        }

        private static Block GenerateBlock(Hash chainId, Hash previousHash, ulong index)
        {
            var block = new Block(previousHash)
            {
                Header = new BlockHeader
                {
                    ChainId = chainId,
                    Index = index,
                    PreviousBlockHash = previousHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    MerkleTreeRootOfWorldState = Hash.Default
                }
            };
            block.FillTxsMerkleTreeRootInHeader();
            return block;
        }
    }
}