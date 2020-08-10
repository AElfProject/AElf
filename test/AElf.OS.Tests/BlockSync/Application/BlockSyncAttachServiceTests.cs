using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Helpers;
using AElf.OS.Network;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncAttachServiceTests : BlockSyncTestBase
    {
        private readonly IBlockSyncAttachService _blockSyncAttachService;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly OSTestHelper _osTestHelper;
        
        public BlockSyncAttachServiceTests()
        {
            _blockSyncAttachService = GetRequiredService<IBlockSyncAttachService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }

        [Fact]
        public async Task AttachBlockWithTransactions_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactions = await _osTestHelper.GenerateTransferTransactions(2);
            var block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            var executedBlock = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
            var blockWithTransactions = new BlockWithTransactions
                {Header = executedBlock.Header, Transactions = {transactions}};

            var attachFinished = false;

            await _blockSyncAttachService.AttachBlockWithTransactionsAsync(blockWithTransactions, "pubkey",
                () =>
                {
                    attachFinished = true;
                    return Task.CompletedTask;
                });
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHash.ShouldBe(blockWithTransactions.GetHash());
            chain.BestChainHeight.ShouldBe(blockWithTransactions.Height);
            attachFinished.ShouldBeTrue();

            var txs = await _blockchainService.GetTransactionsAsync(transactions.Select(t => t.GetHash()));
            txs.Count.ShouldBe(2);
        }
    }
}