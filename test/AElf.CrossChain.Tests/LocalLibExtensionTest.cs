using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public sealed class LocalLibExtensionTest : CrossChainWithChainTestBase
    {
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ITransactionManager _transactionManager;
        private readonly IBlockchainService _blockchainService;
        
        public LocalLibExtensionTest()
        {
            _transactionManager = GetRequiredService<ITransactionManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blockchainService = GetService<IBlockchainService>();
        }
        
        [Fact]
        public async Task GetLibHeight_Test()
        {
            var lastIrreversibleBlockDto = await _blockchainService.GetLibHashAndHeightAsync();
            lastIrreversibleBlockDto.BlockHeight.ShouldBe(5);
        }

        [Fact]
        public async Task GetIrreversibleBlockByHeight_Test()
        {
            var height = 6;
            var irreversibleBlock = await _blockchainService.GetIrreversibleBlockByHeightAsync(height);
            irreversibleBlock.ShouldBeNull();

            height = 4;
            irreversibleBlock = await _blockchainService.GetIrreversibleBlockByHeightAsync(height);
            var block = _kernelTestHelper.BestBranchBlockList[height - 1];
            var body = block.Body;
            foreach (var txId in body.Transactions)
            {
                var tx = await _transactionManager.GetTransaction(txId);
                body.TransactionList.Add(tx);
            }
            irreversibleBlock.Equals(block).ShouldBeTrue();
        }
    }
}