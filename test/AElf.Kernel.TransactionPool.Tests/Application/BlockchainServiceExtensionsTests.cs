using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Application
{
    public class BlockchainServiceExtensionsTests : TransactionPoolWithChainTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;

        public BlockchainServiceExtensionsTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task GetBlockIndexes_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var result = await _blockchainService.GetBlockIndexesAsync(0, chain.BestChainHash, chain.BestChainHeight);
            result.Count.ShouldBe(0);

            result = await _blockchainService.GetBlockIndexesAsync(9, chain.BestChainHash, chain.BestChainHeight);
            result.Count.ShouldBe(3);
            result.ShouldContain(new BlockIndex(chain.BestChainHash, chain.BestChainHeight));
            result.ShouldContain(new BlockIndex(_kernelTestHelper.BestBranchBlockList[9].GetHash(),
                _kernelTestHelper.BestBranchBlockList[9].Height));
            result.ShouldContain(new BlockIndex(_kernelTestHelper.BestBranchBlockList[8].GetHash(),
                _kernelTestHelper.BestBranchBlockList[8].Height));
        }
    }
}