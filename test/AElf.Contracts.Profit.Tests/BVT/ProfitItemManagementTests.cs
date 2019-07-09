using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit.BVT
{
    public partial class ProfitContractTests : ProfitContractTestBase
    {
        protected Hash DefaultSchemeId { get; set; }

        public ProfitContractTests()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task ProfitContract_CreateScheme()
        {
            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorMinerKeyPair[0].PublicKey);

            await creator.CreateScheme.SendAsync(new CreateSchemeInput
            {
                ProfitReceivingDuePeriodCount = 100,
            });

            var createdSchemeIds = (await creator.GetCreatedSchemeIds.CallAsync(new GetCreatedSchemeIdsInput
            {
                Creator = creatorAddress
            })).SchemeIds;

            createdSchemeIds.Count.ShouldBe(1);

            DefaultSchemeId = createdSchemeIds.First();

            await ProfitContractStub.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = DefaultSchemeId,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Amount = (long) (ProfitContractTestConsts.NativeTokenTotalSupply * 0.2),
            });

            var treasury = await ProfitContractStub.GetScheme.CallAsync(DefaultSchemeId);

            treasury.Creator.ShouldBe(Address.FromPublicKey(StarterKeyPair.PublicKey));
            treasury.UndistributedProfits[ProfitContractTestConsts.NativeTokenSymbol]
                .ShouldBe((long) (ProfitContractTestConsts.NativeTokenTotalSupply * 0.2));

            var treasuryAddress = await ProfitContractStub.GetSchemeAddress.CallAsync(
                new SchemePeriod
                {
                    SchemeId = DefaultSchemeId
                });
            var treasuryBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Owner = treasuryAddress
            })).Balance;

            treasuryBalance.ShouldBe((long) (ProfitContractTestConsts.NativeTokenTotalSupply * 0.2));

            var schemeId = createdSchemeIds.First();
            var profitItem = await creator.GetScheme.CallAsync(schemeId);

            profitItem.Creator.ShouldBe(creatorAddress);
            profitItem.CurrentPeriod.ShouldBe(1);
            profitItem.ProfitReceivingDuePeriodCount.ShouldBe(ProfitContractConsts
                .DefaultProfitReceivingDuePeriodCount);
            profitItem.TotalShares.ShouldBe(0);
            profitItem.UndistributedProfits.Count.ShouldBe(0);

            var itemBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Owner = profitItem.VirtualAddress
            })).Balance;

            Assert.Equal(0, itemBalance);
        }
    }
}