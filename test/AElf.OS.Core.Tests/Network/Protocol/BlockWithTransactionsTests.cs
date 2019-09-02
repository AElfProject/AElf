using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Protocol
{
    public class BlockWithTransactionsTests : NetworkInfrastructureTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;

        public BlockWithTransactionsTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task BlockWithTransactions_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactions = new List<Transaction>();
            for (var i = 0; i < 5; i++)
            {
                transactions.Add(_kernelTestHelper.GenerateTransaction());
            }

            var block = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash, transactions);
            
            var blockWithTransactions = new BlockWithTransactions
            {
                Header = block.Header,
                Transactions = { transactions }
            };
            
            blockWithTransactions.FullTransactionList.ShouldBe(transactions);
            blockWithTransactions.TransactionIds.ShouldBe(transactions.Select(o=>o.GetHash()));
            blockWithTransactions.Body.BlockHeader.ShouldBe(blockWithTransactions.Header.GetHash());
        }
    }
}