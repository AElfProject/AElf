using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Configuration;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.TestBase;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public sealed class BlockTransactionLimitTests : AElfIntegratedTest<KernelTestAElfModule>
    {
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly IConfigurationProcessor _blockTransactionLimitConfigurationProcessor;
        private readonly IBlockStateSetManger _blockStateSetManger;

        public BlockTransactionLimitTests()
        {
            _blockTransactionLimitProvider = GetRequiredService<IBlockTransactionLimitProvider>();
            _blockTransactionLimitConfigurationProcessor = GetRequiredService<IConfigurationProcessor>();
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

        [Fact]
        public async Task TransactionLimitConfigurationProcessorTest()
        {
            var blockTransactionLimit = 50;
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
            await _blockTransactionLimitConfigurationProcessor.ProcessConfigurationAsync(new Int32Value
            {
                Value = blockTransactionLimit
            }.ToByteString(), blockIndex);
            
            var limit = await _blockTransactionLimitProvider.GetLimitAsync(
                new ChainContext
                {
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight
                });
            limit.ShouldBe(blockTransactionLimit);
        }
    }
}