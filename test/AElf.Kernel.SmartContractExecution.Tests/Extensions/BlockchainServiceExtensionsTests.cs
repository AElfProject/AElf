using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Extensions
{
    public sealed class BlockchainServiceExtensionsTests : SmartContractExecutionTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;

        public BlockchainServiceExtensionsTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task GetBlocksAsync_Test()
        {
            var blockHashes = new List<Hash>();
            var chain = await _kernelTestHelper.MockChainAsync();
            var blockHash = chain.BestChainHash;
            for (var i = 0; i < chain.BestChainHeight; i++)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockHash);
                if(block == null) break;
                blockHashes.Add(blockHash);
                blockHash = block.Header.PreviousBlockHash;
            }

            var blocks = await _blockchainService.GetBlocksAsync(blockHashes);
            blocks.Count.ShouldBe(blockHashes.Count);
        }
    }
}