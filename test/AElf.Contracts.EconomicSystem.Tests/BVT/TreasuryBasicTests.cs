using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using Acs5;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Parliament;
using AElf.Contracts.Treasury;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
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
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress,
                nameof(TreasuryContractStub.SetVoteWeightInterest), newInterest);
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
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress, nameof(TreasuryContractStub.ChangeTreasuryController), newAuthority);
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

            var proposalHash = Hash.Parser.ParseFrom(createResult.TransactionResult.ReturnValue);
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

        [Fact]
        public async Task Treasury_Dividend_Pool_Weight_Update_Test()
        {
            var defaultWeightSetting = await TreasuryContractStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
            defaultWeightSetting.BackupSubsidyProportion.ShouldBe(5);
            defaultWeightSetting.CitizenWelfareProportion.ShouldBe(75);
            defaultWeightSetting.MinerRewardProportion.ShouldBe(20);
            var newWeightSetting = new DividendPoolWeightSetting
            {
                BackupSubsidyWeight = 1,
                CitizenWelfareWeight = 1,
                MinerRewardWeight = 8
            };
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress, nameof(TreasuryContractStub.SetDividendPoolWeightSetting), newWeightSetting);
            var updatedWeightSetting = await TreasuryContractStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
            updatedWeightSetting.BackupSubsidyProportion.ShouldBe(10);
            updatedWeightSetting.CitizenWelfareProportion.ShouldBe(10);
            updatedWeightSetting.MinerRewardProportion.ShouldBe(80);
            
            var treasuryProfit = await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.Treasury]);
            var subSchemes = treasuryProfit.SubSchemes;
            subSchemes.Count.ShouldBe(3);
            var weightOneCount = subSchemes.Count(x => x.Shares == 1);
            weightOneCount.ShouldBe(2);
            var weightEightCount = subSchemes.Count(x => x.Shares == 8);
            weightEightCount.ShouldBe(1);
        }
        
        [Fact]
        public async Task Treasury_Dividend_Pool_Weight_Update_To_Miner_Reward_Weight_Test()
        {
            var newWeightSetting = new DividendPoolWeightSetting
            {
                BackupSubsidyWeight = 1,
                CitizenWelfareWeight = 1,
                MinerRewardWeight = 8
            };
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress, nameof(TreasuryContractStub.SetDividendPoolWeightSetting), newWeightSetting);
            var defaultWeightSetting = await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            defaultWeightSetting.BasicMinerRewardProportion.ShouldBe(50);
            defaultWeightSetting.ReElectionRewardProportion.ShouldBe(25);
            defaultWeightSetting.VotesWeightRewardProportion.ShouldBe(25);
        }

        [Fact]
        public async Task Miner_Reward_Weight_Update_Test()
        {
            var defaultWeightSetting = await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            defaultWeightSetting.BasicMinerRewardProportion.ShouldBe(50);
            defaultWeightSetting.ReElectionRewardProportion.ShouldBe(25);
            defaultWeightSetting.VotesWeightRewardProportion.ShouldBe(25);
            var newWeightSetting = new MinerRewardWeightSetting
            {
                BasicMinerRewardWeight = 1,
                ReElectionRewardWeight = 1,
                VotesWeightRewardWeight = 8
            };
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress,
                nameof(TreasuryContractStub.SetMinerRewardWeightSetting), newWeightSetting);
            var updatedWeightSetting = await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            updatedWeightSetting.BasicMinerRewardProportion.ShouldBe(10);
            updatedWeightSetting.ReElectionRewardProportion.ShouldBe(10);
            updatedWeightSetting.VotesWeightRewardProportion.ShouldBe(80);

            var minerRewardProfit =
                await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.MinerReward]);
            var subSchemes = minerRewardProfit.SubSchemes;
            subSchemes.Count.ShouldBe(3);
            var weightOneCount = subSchemes.Count(x => x.Shares == 1);
            weightOneCount.ShouldBe(2);
            var weightEightCount = subSchemes.Count(x => x.Shares == 8);
            weightEightCount.ShouldBe(1);
        }
    }
}