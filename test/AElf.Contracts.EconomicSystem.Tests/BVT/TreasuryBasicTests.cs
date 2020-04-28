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
            defaultWeightSetting.BackupSubsidyProportionInfo.Proportion.ShouldBe(5);
            defaultWeightSetting.CitizenWelfareProportionInfo.Proportion.ShouldBe(75);
            defaultWeightSetting.MinerRewardProportionInfo.Proportion.ShouldBe(20);
            var newWeightSetting = new DividendPoolWeightSetting
            {
                BackupSubsidyWeight = 1,
                CitizenWelfareWeight = 1,
                MinerRewardWeight = 8
            };
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress, nameof(TreasuryContractStub.SetDividendPoolWeightSetting), newWeightSetting);
            var updatedWeightSetting = await TreasuryContractStub.GetDividendPoolWeightProportion.CallAsync(new Empty());
            updatedWeightSetting.BackupSubsidyProportionInfo.Proportion.ShouldBe(10);
            updatedWeightSetting.CitizenWelfareProportionInfo.Proportion.ShouldBe(10);
            updatedWeightSetting.MinerRewardProportionInfo.Proportion.ShouldBe(80);
            var treasuryProfit = await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.Treasury]);
            var subSchemes = treasuryProfit.SubSchemes;
            subSchemes.Count.ShouldBe(3);
            var backSubsidyScheme = subSchemes.Single(x => x.SchemeId == updatedWeightSetting.BackupSubsidyProportionInfo.SchemeId);
            backSubsidyScheme.Shares.ShouldBe(1);
            var citizenWelfareScheme = subSchemes.Single(x => x.SchemeId == updatedWeightSetting.CitizenWelfareProportionInfo.SchemeId);
            citizenWelfareScheme.Shares.ShouldBe(1);
            var minerRewardScheme = subSchemes.Single(x => x.SchemeId == updatedWeightSetting.MinerRewardProportionInfo.SchemeId);
            minerRewardScheme.Shares.ShouldBe(8);
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
            var minerRewardProfit =
                await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.MinerReward]);
            var subSchemes = minerRewardProfit.SubSchemes;
            subSchemes.Count.ShouldBe(3);
            var minerRewardWeightSetting = await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            var basicMinerRewardScheme = subSchemes.Single(x =>
                x.SchemeId == minerRewardWeightSetting.BasicMinerRewardProportionInfo.SchemeId);
            basicMinerRewardScheme.Shares.ShouldBe(2);
            var reElectionRewardScheme = subSchemes.Single(x =>
                x.SchemeId == minerRewardWeightSetting.ReElectionRewardProportionInfo.SchemeId);
            reElectionRewardScheme.Shares.ShouldBe(1);
            var votesWeightRewardScheme = subSchemes.Single(x =>
                x.SchemeId == minerRewardWeightSetting.VotesWeightRewardProportionInfo.SchemeId);
            votesWeightRewardScheme.Shares.ShouldBe(1);
        }

        [Fact]
        public async Task Miner_Reward_Weight_Update_Test()
        {
            var defaultWeightSetting = await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            defaultWeightSetting.BasicMinerRewardProportionInfo.Proportion.ShouldBe(50);
            defaultWeightSetting.ReElectionRewardProportionInfo.Proportion.ShouldBe(25);
            defaultWeightSetting.VotesWeightRewardProportionInfo.Proportion.ShouldBe(25);
            var newWeightSetting = new MinerRewardWeightSetting
            {
                BasicMinerRewardWeight = 1,
                ReElectionRewardWeight = 1,
                VotesWeightRewardWeight = 8
            };
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress,
                nameof(TreasuryContractStub.SetMinerRewardWeightSetting), newWeightSetting);
            var updatedWeightSetting = await TreasuryContractStub.GetMinerRewardWeightProportion.CallAsync(new Empty());
            updatedWeightSetting.BasicMinerRewardProportionInfo.Proportion.ShouldBe(10);
            updatedWeightSetting.ReElectionRewardProportionInfo.Proportion.ShouldBe(10);
            updatedWeightSetting.VotesWeightRewardProportionInfo.Proportion.ShouldBe(80);
            var minerRewardProfit =
                await ProfitContractStub.GetScheme.CallAsync(ProfitItemsIds[ProfitType.MinerReward]);
            var subSchemes = minerRewardProfit.SubSchemes;
            subSchemes.Count.ShouldBe(3);
            var basicMinerRewardScheme = subSchemes.Single(x =>
                x.SchemeId == updatedWeightSetting.BasicMinerRewardProportionInfo.SchemeId);
            basicMinerRewardScheme.Shares.ShouldBe(1);
            var reElectionRewardScheme = subSchemes.Single(x =>
                x.SchemeId == updatedWeightSetting.ReElectionRewardProportionInfo.SchemeId);
            reElectionRewardScheme.Shares.ShouldBe(1);
            var votesWeightRewardScheme = subSchemes.Single(x =>
                x.SchemeId == updatedWeightSetting.VotesWeightRewardProportionInfo.SchemeId);
            votesWeightRewardScheme.Shares.ShouldBe(8);
        }
    }
}