using System.Threading.Tasks;
using Acs5;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Treasury;
using AElf.Types;
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
            await ExecuteProposalTransaction(Tester, TreasuryContractAddress, nameof(TreasuryContractStub.SetControllerForManageVoteWeightInterest), BootMinerAddress);
            updateInterestRet = (await TreasuryContractStub.SetVoteWeightInterest.SendAsync(newInterest)).TransactionResult;
            updateInterestRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var interestList = await TreasuryContractStub.GetVoteWeightSetting.CallAsync(new Empty());
            interestList.VoteWeightInterestInfos.Count.ShouldBe(1);
            interestList.VoteWeightInterestInfos[0].Capital.ShouldBe(1000);
            interestList.VoteWeightInterestInfos[0].Interest.ShouldBe(16);
            interestList.VoteWeightInterestInfos[0].Day.ShouldBe(400);
        }
    }
}