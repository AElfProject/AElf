using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public sealed class LocalLibServiceTest : CrossChainWithChainTestBase
    {
        private readonly ILocalLibService _localLibService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ITransactionManager _transactionManager;
        
        public LocalLibServiceTest()
        {
            _localLibService = GetRequiredService<ILocalLibService>();
            _transactionManager = GetRequiredService<ITransactionManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }
        
        [Fact]
        public async Task GetLibHeight_Test()
        {
            var libHeight = await _localLibService.GetLibHeight();
            libHeight.ShouldBe(5);
        }

        [Fact]
        public async Task GetIrreversibleBlockByHeight_Test()
        {
            var height = 6;
            var irreversibleBlock = await _localLibService.GetIrreversibleBlockByHeightAsync(height);
            irreversibleBlock.ShouldBeNull();

            height = 4;
            irreversibleBlock = await _localLibService.GetIrreversibleBlockByHeightAsync(height);
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