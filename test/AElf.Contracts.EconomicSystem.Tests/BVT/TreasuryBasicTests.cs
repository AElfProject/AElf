using System.Threading.Tasks;
using Acs1;
using Acs3;
using Acs5;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Treasury;
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

            // Token Converter Contract created AETC token.
            var tokenInformation = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = EconomicSystemTestConstants.ConverterTokenSymbol
            });
            tokenInformation.Issuer.ShouldBe(TokenConverterContractAddress);
            tokenInformation.TotalSupply.ShouldBe(EconomicSystemTestConstants.TotalSupply);
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
        public async Task ModifyVoteInterest_Test()
        {
            var interestList = await TreasuryContractStub.GetVoteWeightSetting.CallAsync(new Empty());
            interestList.VoteWeightInterestInfos.Count.ShouldBe(3);
            var newInterest = new VoteWeightInterestList();
            newInterest.VoteWeightInterestInfos.Add(new VoteWeightInterest
            {
                Capital = 1000,
                Interest = 16,
                Day = 400
            });
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress, nameof(TreasuryContractStub.SetVoteWeightInterest), newInterest);
            interestList = await TreasuryContractStub.GetVoteWeightSetting.CallAsync(new Empty());
            interestList.VoteWeightInterestInfos.Count.ShouldBe(1);
            interestList.VoteWeightInterestInfos[0].Capital.ShouldBe(1000);
            interestList.VoteWeightInterestInfos[0].Interest.ShouldBe(16);
            interestList.VoteWeightInterestInfos[0].Day.ShouldBe(400);
        }

        [Fact]
        public async Task TransferAuthorizationForVoteInterest_Test()
        {
            var newInterest = new VoteWeightInterestList();
            newInterest.VoteWeightInterestInfos.Add(new VoteWeightInterest
            {
                Capital = 1000,
                Interest = 16,
                Day = 400
            });
            var updateInterestRet = (await TreasuryContractStub.SetVoteWeightInterest.SendAsync(newInterest)).TransactionResult;
            updateInterestRet.Status.ShouldBe(TransactionResultStatus.Failed);
            var newParliament = new Parliament.CreateOrganizationInput
            {
                ProposerAuthorityRequired = false,
                ProposalReleaseThreshold = new Acs3.ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ParliamentMemberProposingAllowed = false
            };
            var bpParliamentStub = GetParliamentContractTester(InitialCoreDataCenterKeyPairs[0]);
            var createNewParliament =
                (await bpParliamentStub.CreateOrganization.SendAsync(newParliament)).TransactionResult;
            createNewParliament.Status.ShouldBe(TransactionResultStatus.Mined);
            var calculatedNewParliamentAddress = await ParliamentContractStub.CalculateOrganizationAddress.CallAsync(newParliament);
            var newAuthority = new AuthorityInfo
            {
                OwnerAddress = calculatedNewParliamentAddress,
                ContractAddress = ParliamentContractAddress
            };
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress, nameof(TreasuryContractStub.ChangeVoteWeightInterestController), newAuthority);
            var proposalToUpdateInterest = new Acs3.CreateProposalInput
            {
                OrganizationAddress = calculatedNewParliamentAddress,
                ContractMethodName = nameof(TreasuryContractContainer.TreasuryContractStub.SetVoteWeightInterest),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = newInterest.ToByteString(),
                ToAddress = TreasuryContractAddress
            };
            var createResult = await bpParliamentStub.CreateProposal.SendAsync(proposalToUpdateInterest);
            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalHash = HashHelper.HexStringToHash(createResult.TransactionResult.ReadableReturnValue.Replace("\"", ""));
            await bpParliamentStub.Approve.SendAsync(proposalHash);
            await ParliamentContractStub.Release.SendAsync(proposalHash);
            var interestList = await TreasuryContractStub.GetVoteWeightSetting.CallAsync(new Empty());
            interestList.VoteWeightInterestInfos.Count.ShouldBe(1);
            interestList.VoteWeightInterestInfos[0].Capital.ShouldBe(1000);
            interestList.VoteWeightInterestInfos[0].Interest.ShouldBe(16);
            interestList.VoteWeightInterestInfos[0].Day.ShouldBe(400);
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