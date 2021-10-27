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
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        public SystemTransactionGenerationServiceTests()
        {
            _systemTransactionGenerationService = GetRequiredService<ISystemTransactionGenerationService>();
        }

        [Fact]
        public async Task Generate_SystemTransactions_Test()
        {
            var transactionList = await _systemTransactionGenerationService.GenerateSystemTransactionsAsync(
                SampleAddress.AddressList[0], 1L, HashHelper.ComputeFrom("hash"));
            transactionList.ShouldNotBeNull();
            transactionList.Count.ShouldBe(2);
        }
    }
}