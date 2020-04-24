using System.Collections.Generic;
using System.Threading.Tasks;
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
        public async Task Generate_SystemTransactions()
        {
            var transactions =
                await _systemTransactionGenerator.GenerateTransactionsAsync(SampleAddress.AddressList[0], 0L,
                    Hash.Empty);
            transactions.Count.ShouldBe(2);
        }
        
        [Fact]
        public async Task Generate_SystemTransactions_Test()
        {
            var transactionList = await _systemTransactionGenerationService.GenerateSystemTransactionsAsync(
                SampleAddress.AddressList[0], 1L, HashHelper.ComputeFromString("hash"));
            transactionList.ShouldNotBeNull();
            transactionList.Count.ShouldBe(2);
        }
    }
}