using System;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class BlockchainStateMergingServiceTests : AElfKernelTestBase
    {
        private IBlockchainStateManager _blockchainStateManager;
        private IBlockchainStateMergingService _blockchainStateMergingService;

        public BlockchainStateMergingServiceTests()
        {
            _blockchainStateManager = GetRequiredService<IBlockchainStateManager>();
            _blockchainStateMergingService = GetRequiredService<IBlockchainStateMergingService>();
        }

        [Fact]
        public async Task BlockState_NoNeed_To_Merge()
        {
            var lastIrreversibleBlockHeight = -2;
            var lastIrreversibleBlockHash = Hash.FromString("hash");

            await _blockchainStateMergingService.MergeBlockStateAsync(lastIrreversibleBlockHeight,
                lastIrreversibleBlockHash);

            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            chainStateInfo.BlockHeight.ShouldNotBe(lastIrreversibleBlockHeight);
            chainStateInfo.MergingBlockHash.ShouldNotBe(lastIrreversibleBlockHash);
        }

        [Fact]
        public async Task BlockState_Merge_GotException()
        {
            var lastIrreversibleBlockHeight = 1;
            var lastIrreversibleBlockHash = Hash.FromString("hash");

            await Should.ThrowAsync<InvalidOperationException>(()=>_blockchainStateMergingService.MergeBlockStateAsync(lastIrreversibleBlockHeight,
                lastIrreversibleBlockHash));
            
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            chainStateInfo.BlockHeight.ShouldNotBe(lastIrreversibleBlockHeight);
            chainStateInfo.MergingBlockHash.ShouldNotBe(lastIrreversibleBlockHash);
        }

        [Fact]
        public async Task BlockState_MergeBlock_Normal()
        {
            var blockStateSet1 = new BlockStateSet()
            {
                BlockHeight = 1,
                BlockHash = Hash.FromString("hash"),
                PreviousHash = Hash.Empty
            };
            var blockStateSet2 = new BlockStateSet()
            {
                BlockHeight = 2,
                BlockHash = Hash.FromString("hash2"),
                PreviousHash = blockStateSet1.BlockHash
            };
            var blockStateSet3 = new BlockStateSet()
            {
                BlockHeight = 3,
                BlockHash = Hash.FromString("hash3"),
                PreviousHash = blockStateSet2.BlockHash
            };

            //test merge block height 1
            {
                await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet1);

                await _blockchainStateMergingService.MergeBlockStateAsync(blockStateSet1.BlockHeight,
                    blockStateSet1.BlockHash);

                var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
                chainStateInfo.BlockHeight.ShouldBe(1);
                chainStateInfo.BlockHash.ShouldBe(blockStateSet1.BlockHash);
            }

            //test merge block height 2
            {
                await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet2);
                await _blockchainStateMergingService.MergeBlockStateAsync(blockStateSet2.BlockHeight,
                    blockStateSet2.BlockHash);

                var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
                chainStateInfo.BlockHeight.ShouldBe(2);
                chainStateInfo.BlockHash.ShouldBe(blockStateSet2.BlockHash);
            }

            //test merge height 3 without block state set before
            {
                await Should.ThrowAsync<InvalidOperationException>(()=> _blockchainStateMergingService.MergeBlockStateAsync(blockStateSet3.BlockHeight,
                    blockStateSet3.BlockHash));

                var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
                chainStateInfo.BlockHeight.ShouldBe(2);
                chainStateInfo.BlockHash.ShouldBe(blockStateSet2.BlockHash);
            }
        }
    }
}