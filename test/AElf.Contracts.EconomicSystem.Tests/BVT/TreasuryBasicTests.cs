using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs5;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Kernel;
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
    }
}