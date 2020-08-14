using System;
using System.Threading.Tasks;
using AElf.Kernel.Configuration;
using AElf.Kernel.SmartContract.Domain;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Miner
{
    public class BlockTransactionLimitConfigurationProcessorTests : KernelTestBase
    {
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly IConfigurationProcessor _blockTransactionLimitConfigurationProcessor;
        private readonly IBlockStateSetManger _blockStateSetManger;
        
        public BlockTransactionLimitConfigurationProcessorTests()
        {
            _blockTransactionLimitProvider = GetRequiredService<IBlockTransactionLimitProvider>();
            _blockTransactionLimitConfigurationProcessor = GetRequiredService<IConfigurationProcessor>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }
        
        [Fact]
        public async Task ProcessConfiguration_Test()
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
            
            var limit = await _blockTransactionLimitProvider.GetLimitAsync(
                new ChainContext
                {
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight
                });
            limit.ShouldBe(int.MaxValue);
            
            await _blockTransactionLimitConfigurationProcessor.ProcessConfigurationAsync(new Int32Value
            {
                Value = -1
            }.ToByteString(), blockIndex);
            
            limit = await _blockTransactionLimitProvider.GetLimitAsync(
                new ChainContext
                {
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight
                });
            limit.ShouldBe(int.MaxValue);

            await _blockTransactionLimitConfigurationProcessor.ProcessConfigurationAsync(new Int32Value
            {
                Value = blockTransactionLimit
            }.ToByteString(), blockIndex);
            
            limit = await _blockTransactionLimitProvider.GetLimitAsync(
                new ChainContext
                {
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight
                });
            limit.ShouldBe(blockTransactionLimit);
        }
    }
}