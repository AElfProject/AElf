using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
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
            Assert.Equal(ProfitContractTestConsts.NativeTokenSymbol, treasury.TokenSymbol);
            
            var treasuryAddress = await ProfitContractStub.GetProfitItemVirtualAddress.CallAsync(new GetProfitItemVirtualAddressInput
            {
                ProfitId = TreasuryHash
            });
            var treasuryBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Owner = treasuryAddress
            })).Balance;
            
            Assert.Equal(ProfitContractTestConsts.NativeTokenTotalSupply * 0.2, treasuryBalance);
        }

        [Fact]
        public async Task ProfitContract_CreateProfitItem()
        {
            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorMinerKeyPair[0].PublicKey);
            
            await creator.CreateProfitItem.SendAsync(new CreateProfitItemInput
            {
                TokenSymbol = ProfitContractTestConsts.NativeTokenSymbol,
            });

            var createdProfitIds = (await creator.GetCreatedProfitItems.CallAsync(new GetCreatedProfitItemsInput
            {
                Creator = creatorAddress
            })).ProfitIds;

            Assert.Single(createdProfitIds);

            var profitId = createdProfitIds.First();
            var profitItem = await creator.GetProfitItem.CallAsync(profitId);

            Assert.Equal(creatorAddress, profitItem.Creator);
            Assert.Equal(ProfitContractTestConsts.NativeTokenSymbol, profitItem.TokenSymbol);
            Assert.Equal(1, profitItem.CurrentPeriod);
            Assert.Equal(ProfitContractConsts.DefaultExpiredPeriodNumber, profitItem.ExpiredPeriodNumber);
            Assert.Equal(0, profitItem.TotalWeight);
        }
    }
}