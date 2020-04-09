using System.Threading.Tasks;
using Acs1;
using Acs3;
using Acs5;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Treasury;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest : EconomicSystemTestBase
    {
        public EconomicSystemTest()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task EconomistSystem_CheckBasicInformation_Test()
        {
            // Treasury contract created Treasury profit scheme and set Profit Id to Profit Contract.
            var treasuryProfit = await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.Treasury]);
            treasuryProfit.Manager.ShouldBe(TreasuryContractAddress);
            treasuryProfit.SubSchemes.Count.ShouldBe(3);
            treasuryProfit.IsReleaseAllBalanceEveryTimeByDefault.ShouldBe(true);
        }

        [Fact]
        public async Task EconomistSystem_SetMethodCallThreshold_Test()
        {
            const long feeAmount = 100L;
            var setMethodResult = await MethodCallThresholdContractStub.SetMethodCallingThreshold.SendAsync(
                new SetMethodCallingThresholdInput
                {
                    Method = nameof(MethodCallThresholdContractStub.SendForFun),
                    SymbolToAmount = {{EconomicSystemTestConstants.NativeTokenSymbol, feeAmount}}
                });
            setMethodResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var tokenAmount = await MethodCallThresholdContractStub.GetMethodCallingThreshold.CallAsync(new StringValue
            {
                Value = nameof(MethodCallThresholdContractStub.SendForFun)
            });
            tokenAmount.SymbolToAmount[EconomicSystemTestConstants.NativeTokenSymbol].ShouldBe(feeAmount);
        }

        [Fact]
        public async Task Treasury_ChangeMethodFeeController_Test()
        {
            var createOrganizationResult = await ParliamentContractStub.CreateOrganization.SendAsync(
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1000,
                        MinimalVoteThreshold = 1000
                    }
                });

            var organizationAddress = createOrganizationResult.Output;

            var methodFeeController = await TreasuryContractStub.GetMethodFeeController.CallAsync(new Empty());
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress,
                nameof(TreasuryContractStub.ChangeMethodFeeController),
                new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = methodFeeController.ContractAddress
                });

            var newMethodFeeController = await TreasuryContractStub.GetMethodFeeController.CallAsync(new Empty());
            newMethodFeeController.OwnerAddress.ShouldBe(organizationAddress);
        }
    }
}