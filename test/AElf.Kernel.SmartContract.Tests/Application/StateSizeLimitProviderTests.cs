using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.TestBase;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class StateSizeLimitProviderTests : AElfIntegratedTest<SmartContractTestAElfModule>
    {
        private readonly IStateSizeLimitProvider _stateSizeLimitProvider;
        private readonly IBlockStateSetManger _blockStateSetManger;

        public StateSizeLimitProviderTests()
        {
            _stateSizeLimitProvider = GetRequiredService<IStateSizeLimitProvider>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }

        [Fact]
        public async Task StateSizeLimitStateProvider_GetAndSet_Test()
        {
            var blockIndex = new BlockIndex
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash"),
                BlockHeight = 1
            };

            await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            });
            
            {
                var limit = await _stateSizeLimitProvider.GetStateSizeLimitAsync(blockIndex);
                limit.ShouldBe(SmartContractConstants.StateSizeLimit);
            }
            
            var expectedLimit = 50;
            
            {
                await _stateSizeLimitProvider.SetStateSizeLimitAsync(blockIndex, expectedLimit);
                var limit = await _stateSizeLimitProvider.GetStateSizeLimitAsync(blockIndex);
                limit.ShouldBe(expectedLimit);
                
                expectedLimit = 1;
                await _stateSizeLimitProvider.SetStateSizeLimitAsync(blockIndex, expectedLimit);
                limit = await _stateSizeLimitProvider.GetStateSizeLimitAsync(blockIndex);
                limit.ShouldBe(expectedLimit);
                
                await _stateSizeLimitProvider.SetStateSizeLimitAsync(blockIndex, 0);
                limit = await _stateSizeLimitProvider.GetStateSizeLimitAsync(
                    new ChainContext
                    {
                        BlockHash = blockIndex.BlockHash,
                        BlockHeight = blockIndex.BlockHeight
                    });
                limit.ShouldBe(expectedLimit);
            }
            
            var blockIndex2 = new BlockIndex
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash1"),
                BlockHeight = blockIndex.BlockHeight + 1
            };
            
            var blockStateSet2 = new BlockStateSet
            {
                PreviousHash = blockIndex.BlockHash,
                BlockHash = blockIndex2.BlockHash,
                BlockHeight = blockIndex2.BlockHeight + 1
            };
            
            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet2);
            
            {
                var stateSizeLimit =
                    await _stateSizeLimitProvider.GetStateSizeLimitAsync(blockIndex2);
                stateSizeLimit.ShouldBe(expectedLimit);
            }
        }
    }
}