using System.Collections.Generic;
using System.Linq;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Kernel;
using AElf.Synchronization.BlockSynchronization;
using Easy.MessageHub;
using Xunit;

namespace AElf.Synchronization.Tests
{
    public class BlockSetTests
    {
        [Fact]
        public void Init_WithGenesisBlock_LibIsGenesis()
        {
            var genesis = SyncTestHelpers.GetGenesisBlock();
            
            BlockSet blockSet = new BlockSet();
            blockSet.Init(SyncTestHelpers.GetRandomMiners().ToPubKeyStrings(), genesis);
            
            Assert.Equal(blockSet.CurrentLib.BlockHash, genesis.GetHash());
        }
        
        [Fact]
        public void PushBlock_PushHigherThanHead_ShouldThrowUnlikable()
        {
            var genesis = SyncTestHelpers.GetGenesisBlock();
            
            BlockSet blockSet = new BlockSet();
            blockSet.Init(SyncTestHelpers.GetRandomMiners().ToPubKeyStrings(), genesis);

            var block1 = SyncTestHelpers.BuildNext(genesis);
            var block2 = SyncTestHelpers.BuildNext(block1);
            
            UnlinkableBlockException e = Assert.Throws<UnlinkableBlockException>(() => blockSet.PushBlock(block2));
            
            Assert.Equal(blockSet.CurrentLib.BlockHash, genesis.GetHash());
        }
        
        [Fact]
        public void BlockStateCheck()
        {
            var genesis = SyncTestHelpers.GetGenesisBlock();
            IBlock block1 = SyncTestHelpers.BuildNext(genesis); // miner 01
            
            BlockState state = new BlockState(block1, null, false, null);
            
            Assert.Equal(state.BlockHash, block1.GetHash());
        }

        [Fact]
        public void RemoveInvalidBlocks_ShouldRemoveBlock()
        {
            var genesis = SyncTestHelpers.GetGenesisBlock();
            
            BlockSet blockSet = new BlockSet();
            blockSet.Init(SyncTestHelpers.GetRandomMiners().ToPubKeyStrings(), genesis);

            var invalidBlock = SyncTestHelpers.BuildNext(genesis);
            
            blockSet.PushBlock(invalidBlock);
            blockSet.RemoveInvalidBlock(invalidBlock.GetHash());
            
            Assert.False(blockSet.IsBlockReceived(invalidBlock));
        }

        [Fact]
        public void GetBranch_WithFork_ShouldReturnFirstHeadsBranch()
        {
            var genesis = SyncTestHelpers.GetGenesisBlock();
            
            BlockSet blockSet = new BlockSet();
            blockSet.Init(SyncTestHelpers.GetRandomMiners().ToPubKeyStrings(), genesis);
            
            IBlock forkRoot = SyncTestHelpers.BuildNext(genesis); // Height 2
            
            IBlock blockForkA = SyncTestHelpers.BuildNext(forkRoot); // Height 3
            IBlock blockForkB = SyncTestHelpers.BuildNext(forkRoot); // Height 3
            
            IBlock blockForkB1 = SyncTestHelpers.BuildNext(blockForkB); // Height 4
            
            blockSet.PushBlock(forkRoot);
            blockSet.PushBlock(blockForkA);
            blockSet.PushBlock(blockForkB);
            blockSet.PushBlock(blockForkB1);
            
            var branch = blockSet.GetBranch(blockSet.GetBlockStateByHash(blockForkB1.GetHash()), blockSet.GetBlockStateByHash(blockForkA.GetHash()));
            
            Assert.Equal(3, branch.Count);
            Assert.Equal(branch.ElementAt(0).BlockHash, blockForkB1.GetHash());
            Assert.Equal(branch.ElementAt(1).BlockHash, blockForkB.GetHash());
            Assert.Equal(branch.ElementAt(2).BlockHash, forkRoot.GetHash());
        }

        [Fact]
        public void ExtendChainToLibBasicTest()
        {
            List<BlockState> eventList = new List<BlockState>();
            
            // 3 miners (self included).
            var minerPubKeys = SyncTestHelpers.GetRandomMiners().ToPubKeyStrings();
            
            var genesis = SyncTestHelpers.GetGenesisBlock();
            
            BlockSet blockSet = new BlockSet();
            blockSet.LibChanged += (sender, args) =>
            {
                if (args is LibChangedArgs e)
                    eventList.Add(e.NewLib);
            };
            
            blockSet.Init(minerPubKeys, genesis); // CurrentLIB = head = genesis

            IBlock block1 = SyncTestHelpers.BuildNext(genesis, minerPubKeys[0]); // miner 01 
            IBlock block2 = SyncTestHelpers.BuildNext(block1, minerPubKeys[1]);  // miner 02
            IBlock block3 = SyncTestHelpers.BuildNext(block2, minerPubKeys[2]);  // miner 03 
            
            IBlock block4 = SyncTestHelpers.BuildNext(block3, minerPubKeys[0]);  // miner 04
            

            
            blockSet.PushBlock(block1);
            blockSet.PushBlock(block2);
            blockSet.PushBlock(block3, true);
            
            Assert.True(eventList.Count == 1);
            
            blockSet.PushBlock(block4);
            
            Assert.True(eventList.Any());
            Assert.Equal(eventList.ElementAt(0).BlockHash, block1.GetHash());
        }
    }
}