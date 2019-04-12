using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Miner.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Miner
{
    public class SystemTransactionGenerationServiceTests: AElfMinerTestBase
    {
        private ISystemTransactionGenerator _systemTransactionGenerator;
        private SystemTransactionGenerationService _systemTransactionGenerationService;
        public SystemTransactionGenerationServiceTests()
        {
            _systemTransactionGenerator = GetRequiredService<ISystemTransactionGenerator>();
            _systemTransactionGenerationService = new SystemTransactionGenerationService(new[] {_systemTransactionGenerator});
        }

        [Fact]
        public void Generate_SystemTransactions()
        {
            var transactions = new List<Transaction>();
            _systemTransactionGenerator.GenerateTransactions(Address.Zero, 0L, Hash.Empty, ref transactions);
            transactions.Count.ShouldBe(2);
        }
        
        [Fact]
        public void Generate_SystemTransactionsTest()
        {
            var transactionList = _systemTransactionGenerationService.GenerateSystemTransactions(
                Address.Generate(), 1L, Hash.Generate());
            transactionList.ShouldNotBeNull();
            transactionList.Count.ShouldBe(2);
        }
    }
}