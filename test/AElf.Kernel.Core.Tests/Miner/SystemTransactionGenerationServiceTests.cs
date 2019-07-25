using System.Collections.Generic;
using AElf.Kernel.Miner.Application;
using AElf.Types;
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
            _systemTransactionGenerator.GenerateTransactions(SampleAddress.AddressList[0], 0L, Hash.Empty, ref transactions);
            transactions.Count.ShouldBe(2);
        }
        
        [Fact]
        public void Generate_SystemTransactionsTest()
        {
            var transactionList = _systemTransactionGenerationService.GenerateSystemTransactions(
                SampleAddress.AddressList[0], 1L, Hash.FromString("hash"));
            transactionList.ShouldNotBeNull();
            transactionList.Count.ShouldBe(2);
        }
    }
}