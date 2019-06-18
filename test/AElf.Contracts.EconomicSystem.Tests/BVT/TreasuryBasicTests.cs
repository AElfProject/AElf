using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken.Messages;
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
        public async Task EconomistSystem_CheckBasicInformation()
        {
            // Treasury contract created Treasury Profit Item and set Profit Id to Profit Contract.
            ProfitItemsIds[ProfitType.Treasury].ShouldBe(await ProfitContractStub.GetTreasuryProfitId.CallAsync(new Empty()));
            var treasuryProfit = await ProfitContractStub.GetProfitItem.CallAsync(ProfitItemsIds[ProfitType.Treasury]);
            treasuryProfit.Creator.ShouldBe(TreasuryContractAddress);
            treasuryProfit.SubProfitItems.Count.ShouldBe(3);
            treasuryProfit.IsReleaseAllBalanceEverytimeByDefault.ShouldBe(true);

            // Token Converter Contract created AETC token.
            var tokenInformation = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = EconomicSystemTestConstants.ConverterTokenSymbol
            });
            tokenInformation.Issuer.ShouldBe(TokenConverterContractAddress);
            tokenInformation.TotalSupply.ShouldBe(EconomicSystemTestConstants.TotalSupply);
        }

        [Fact]
        public async Task EconomistSystem_SetMethodTransactionFee()
        {
            const long feeAmount = 10L;
            await TransactionFeeChargingContractStub.SetMethodFee.SendAsync(new SetMethodFeeInput
            {
                Method = nameof(TransactionFeeChargingContractStub.SendForFun),
                SymbolToAmount = {{EconomicSystemTestConstants.NativeTokenSymbol, feeAmount}}
            });

            var tokenAmount = await TransactionFeeChargingContractStub.GetMethodFee.CallAsync(new MethodName
                {Name = nameof(TransactionFeeChargingContractStub.SendForFun)});
            tokenAmount.SymbolToAmount[EconomicSystemTestConstants.NativeTokenSymbol].ShouldBe(feeAmount);
        }

        [Fact]
        public async Task EconomistSystem_ChargeMethodTransactionFee()
        {
            
        }
    }
}