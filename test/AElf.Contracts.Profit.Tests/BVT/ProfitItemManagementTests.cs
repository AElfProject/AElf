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
        protected Hash DefaultProfitId { get; set; }

        public ProfitContractTests()
        {
            InitializeContracts();
        }
        
        [Fact]
        public async Task ProfitContract_CreateProfitItem()
        {
            await ProfitContractStub.AddProfits.SendAsync(new AddProfitsInput
            {
                ProfitId = DefaultProfitId,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Amount = (long) (ProfitContractTestConsts.NativeTokenTotalSupply * 0.2),
            });

            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorMinerKeyPair[0].PublicKey);

            await creator.CreateProfitItem.SendAsync(new CreateProfitItemInput
            {
                ProfitReceivingDuePeriodCount = 100,
            });

            var createdProfitIds = (await creator.GetCreatedProfitIds.CallAsync(new GetCreatedProfitIdsInput
            {
                Creator = creatorAddress
            })).ProfitIds;

            createdProfitIds.Count.ShouldBe(1);

            DefaultProfitId = createdProfitIds.First();

            var profitId = createdProfitIds.First();
            var profitItem = await creator.GetProfitItem.CallAsync(profitId);

            profitItem.Creator.ShouldBe(creatorAddress);
            profitItem.CurrentPeriod.ShouldBe(1);
            profitItem.ProfitReceivingDuePeriodCount.ShouldBe(ProfitContractConsts.DefaultProfitReceivingDuePeriodCount);
            profitItem.TotalWeight.ShouldBe(0);
            profitItem.TotalAmounts.Count.ShouldBe(0);

            var itemBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Owner = profitItem.VirtualAddress
            })).Balance;

            Assert.Equal(0, itemBalance);
        }
    }
}