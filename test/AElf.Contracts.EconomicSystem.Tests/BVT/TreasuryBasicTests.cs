using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs5;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
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
            // Treasury contract created Treasury Profit Item and set Profit Id to Profit Contract.
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
        public async Task EconomistSystem_SetMethodTransactionFee_Test()
        {
            const long feeAmount = 10L;
            await TransactionFeeChargingContractStub.SetMethodFee.SendAsync(new TokenAmounts
            {
                Method = nameof(TransactionFeeChargingContractStub.SendForFun),
                Amounts =
                {
                    new TokenAmount
                    {
                        Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                        Amount = feeAmount
                    }
                }
            });

            var tokenAmount = await TransactionFeeChargingContractStub.GetMethodFee.CallAsync(new MethodName
            {
                Name = nameof(TransactionFeeChargingContractStub.SendForFun)
            });
            tokenAmount.Amounts.First(a => a.Symbol == EconomicSystemTestConstants.NativeTokenSymbol).Amount
                .ShouldBe(feeAmount);
        }

        [Fact]
        public async Task EconomistSystem_ChargeMethodTransactionFee_Test()
        {
            await EconomistSystem_SetMethodTransactionFee_Test();

            var chosenOneKeyPair = CoreDataCenterKeyPairs.First();
            var chosenOneAddress = Address.FromPublicKey(chosenOneKeyPair.PublicKey);
            var balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = chosenOneAddress,
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            var tycoon = GetTransactionFeeChargingContractTester(chosenOneKeyPair);
            await tycoon.SendForFun.SendAsync(new Empty());
            var balanceAfter = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = chosenOneAddress,
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            balanceAfter.Balance.ShouldBeLessThan(balanceBefore.Balance);
        }

        [Fact]
        public async Task<long> EconomistSystem_SetMethodTransactionFee_MultipleSymbol_Test()
        {
            const long feeAmount = 10L;

            await TransactionFeeChargingContractStub.SetMethodFee.SendAsync(new TokenAmounts
            {
                Method = nameof(TransactionFeeChargingContractStub.SendForFun),
                Amounts =
                {
                    new TokenAmount
                    {
                        Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol,
                        Amount = feeAmount
                    },
                    new TokenAmount
                    {
                        Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                        Amount = feeAmount
                    }
                }
            });

            var tokenAmount = await TransactionFeeChargingContractStub.GetMethodFee.CallAsync(new MethodName
            {
                Name = nameof(TransactionFeeChargingContractStub.SendForFun)
            });
            tokenAmount.Amounts.First(a => a.Symbol == EconomicSystemTestConstants.NativeTokenSymbol).Amount
                .ShouldBe(feeAmount);
            tokenAmount.Amounts
                .First(a => a.Symbol == EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol).Amount
                .ShouldBe(feeAmount);

            return feeAmount;
        }

        [Fact]
        public async Task EconomicSystem_ChargeMethodTransactionFee_MultipleSymbol_Test()
        {
            var feeAmount = await EconomistSystem_SetMethodTransactionFee_MultipleSymbol_Test();

            var chosenOneKeyPair = CoreDataCenterKeyPairs.First();
            var chosenOneAddress = Address.FromPublicKey(chosenOneKeyPair.PublicKey);
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = chosenOneAddress,
                    Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol
                });
                balance.Balance.ShouldBe(0);
            }
            
            // The chosen one decide to buy some TFCC tokens.
            var chosenOneTokenContractStub =
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, chosenOneKeyPair);
            await chosenOneTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = TokenConverterContractAddress,
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Amount = 100_000_00000000// Enough,
            });
            var chosenOneTokenConverterContractStub =
                GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                    chosenOneKeyPair);
            var result = await chosenOneTokenConverterContractStub.Buy.SendAsync(new BuyInput
            {
                Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol,
                Amount = feeAmount
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = chosenOneAddress,
                    Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol
                });
                balance.Balance.ShouldBe(feeAmount);
            }
            
            var tycoon = GetTransactionFeeChargingContractTester(chosenOneKeyPair);
            await tycoon.SendForFun.SendAsync(new Empty());
            
            {
                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = chosenOneAddress,
                    Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol
                });
                balance.Balance.ShouldBe(0L);
            }
        }
        
        [Fact]
        public async Task EconomistSystem_SetMethodCallThreshold_Test()
        {
            const long feeAmount = 100L;
            var setMethodResult = await MethodCallThresholdContractStub.SetMethodCallingThreshold.SendAsync(new SetMethodCallingThresholdInput
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