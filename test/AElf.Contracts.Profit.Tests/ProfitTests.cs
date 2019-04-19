using System;
using System.Threading.Tasks;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Profit
{
    public class ProfitTests : ProfitContractTestBase
    {
        public ProfitTests()
        {
            InitializeContracts();
            AsyncHelper.RunSync(CreateTreasury);
        }

        [Fact]
        public async Task ProfitContract_CheckTreasury()
        {
            var treasury = await ProfitContractStub.GetProfitItem.CallAsync(TreasuryHash);
            Assert.Equal(Address.FromPublicKey(StarterKeyPair.PublicKey), treasury.Creator);
        }
    }
}