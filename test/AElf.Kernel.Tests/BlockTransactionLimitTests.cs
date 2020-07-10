using System.Threading.Tasks;
using AElf.Kernel.Configuration;
using AElf.Kernel.Miner;
using AElf.TestBase;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public sealed class BlockTransactionLimitTests : AElfIntegratedTest<BlockTransactionLimitExecutedModule>
    {
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly IConfigurationProcessor _blockTransactionLimitConfigurationProcessor;
        
        public BlockTransactionLimitTests()
        {
            _blockTransactionLimitProvider = GetRequiredService<IBlockTransactionLimitProvider>();
            _blockTransactionLimitConfigurationProcessor = GetRequiredService<IConfigurationProcessor>();
        }
        
        [Fact]
        public async Task TransactionLimitSetAndGet_Test()
        {
            var blockTransactionLimit = 50;
            var blockIndex = new BlockIndex
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash"),
                BlockHeight = 1
            };
            await _blockTransactionLimitProvider.SetLimitAsync(blockIndex, blockTransactionLimit);
            var limit = await _blockTransactionLimitProvider.GetLimitAsync(
                new ChainContext
                {
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight
                });
            limit.ShouldBe(blockTransactionLimit);
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