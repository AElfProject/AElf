using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Domain;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public sealed class BlockTransactionLimitProviderTests : KernelTestBase
    {
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly IBlockStateSetManger _blockStateSetManger;

        public BlockTransactionLimitProviderTests()
        {
            _blockTransactionLimitProvider = GetRequiredService<IBlockTransactionLimitProvider>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }
        
        [Fact]
        public async Task TransactionLimitSetAndGet_Test()
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

            var blockTransactionLimit = 50;

            {
                var limit = await _blockTransactionLimitProvider.GetLimitAsync(
                    new ChainContext
                    {
                        BlockHash = blockIndex.BlockHash,
                        BlockHeight = blockIndex.BlockHeight
                    });
                limit.ShouldBe(int.MaxValue);
            }

            {
                await _blockTransactionLimitProvider.SetLimitAsync(blockIndex, blockTransactionLimit);
                var limit = await _blockTransactionLimitProvider.GetLimitAsync(
                    new ChainContext
                    {
                        BlockHash = blockIndex.BlockHash,
                        BlockHeight = blockIndex.BlockHeight
                    });
                limit.ShouldBe(blockTransactionLimit);
            }

            var blockTransactionLimitLessThanSystemTransaction =
                GetRequiredService<IEnumerable<ISystemTransactionGenerator>>().Count();
            await _blockTransactionLimitProvider.SetLimitAsync(blockIndex, blockTransactionLimitLessThanSystemTransaction);
            var limit2 = await _blockTransactionLimitProvider.GetLimitAsync(
                new ChainContext
                {
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight
                });
            limit2.ShouldBe(blockTransactionLimit);
        }
    }
}