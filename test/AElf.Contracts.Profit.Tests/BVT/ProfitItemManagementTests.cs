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
            var creatorAddress = Address.FromPublicKey(CreatorKeyPair[0].PublicKey);

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

            const long contributeAmount = (long) (ProfitContractTestConsts.NativeTokenTotalSupply * 0.1);

            await ProfitContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                SchemeId = DefaultSchemeId,
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Amount = contributeAmount
            });

            var scheme = await ProfitContractStub.GetScheme.CallAsync(DefaultSchemeId);

            scheme.Creator.ShouldBe(Address.FromPublicKey(CreatorKeyPair[0].PublicKey));
            scheme.UndistributedProfits[ProfitContractTestConsts.NativeTokenSymbol]
                .ShouldBe(contributeAmount);

            var schemeAddress = await ProfitContractStub.GetSchemeAddress.CallAsync(
                new SchemePeriod
                {
                    SchemeId = DefaultSchemeId
                });
            var schemeBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ProfitContractTestConsts.NativeTokenSymbol,
                Owner = schemeAddress
            })).Balance;

            schemeBalance.ShouldBe(contributeAmount);
        }
    }
}