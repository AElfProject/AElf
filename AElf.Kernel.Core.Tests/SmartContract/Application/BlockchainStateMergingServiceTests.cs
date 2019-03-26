using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Domain;
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
            var lastIrreversibleBlockHash = Hash.Generate();

            await _blockchainStateMergingService.MergeBlockStateAsync(lastIrreversibleBlockHeight, lastIrreversibleBlockHash);
            
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            chainStateInfo.BlockHeight.ShouldNotBe(lastIrreversibleBlockHeight);
            chainStateInfo.MergingBlockHash.ShouldNotBe(lastIrreversibleBlockHash);
        }

        [Fact]
        public async Task BlockState_Merge_GotException()
        {
            var lastIrreversibleBlockHeight = 1;
            var lastIrreversibleBlockHash = Hash.Generate();
            
            await _blockchainStateMergingService.MergeBlockStateAsync(lastIrreversibleBlockHeight, lastIrreversibleBlockHash);
            
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            chainStateInfo.BlockHeight.ShouldNotBe(lastIrreversibleBlockHeight);
            chainStateInfo.MergingBlockHash.ShouldNotBe(lastIrreversibleBlockHash);
        }
        
        [Fact]
        public async Task BlockState_MergeBlock_Normal()
        {
            var blockStateSet = new BlockStateSet()
            {
                BlockHeight = 1,
                BlockHash = Hash.Generate(),
                PreviousHash = Hash.Empty
            };
            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);
            
            await _blockchainStateMergingService.MergeBlockStateAsync(blockStateSet.BlockHeight, blockStateSet.BlockHash);
            
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            chainStateInfo.BlockHeight.ShouldBe(1);
            chainStateInfo.BlockHash.ShouldBe(blockStateSet.BlockHash);
        }
    }
}