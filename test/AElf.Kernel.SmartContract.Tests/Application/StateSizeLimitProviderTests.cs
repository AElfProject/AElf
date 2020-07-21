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
            
            {
                var stateSizeLimit = 50;
                await _stateSizeLimitProvider.SetStateSizeLimitAsync(blockIndex, stateSizeLimit);
                var limit = await _stateSizeLimitProvider.GetStateSizeLimitAsync(blockIndex);
                limit.ShouldBe(stateSizeLimit);
                
                stateSizeLimit = 1;
                await _stateSizeLimitProvider.SetStateSizeLimitAsync(blockIndex, stateSizeLimit);
                limit = await _stateSizeLimitProvider.GetStateSizeLimitAsync(blockIndex);
                limit.ShouldBe(stateSizeLimit);
                
                await _stateSizeLimitProvider.SetStateSizeLimitAsync(blockIndex, 0);
                limit = await _stateSizeLimitProvider.GetStateSizeLimitAsync(
                    new ChainContext
                    {
                        BlockHash = blockIndex.BlockHash,
                        BlockHeight = blockIndex.BlockHeight
                    });
                limit.ShouldBe(stateSizeLimit);
            }
        }
    }
}