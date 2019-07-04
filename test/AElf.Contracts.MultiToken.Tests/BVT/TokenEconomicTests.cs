using System.Collections.Generic;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests
    {
        private Connector RamConnector = new Connector
        {
            Symbol = "AETC",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };
        
        private async Task InitialEconomic()
        {
            {
                var result =
                    await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
                CheckResult(result.TransactionResult);
            }
            {
                var result =
                    await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(
                        new Empty());
                CheckResult(result.TransactionResult);
            }
            
            {
                var result =(await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    TokenName = "Native Token",
                    TotalSupply = AliceCoinTotalAmount,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = DefaultAddress,
                    LockWhiteList =
                    {
                        ProfitContractAddress,
                        TreasuryContractAddress
                    }
                })).TransactionResult;
                CheckResult(result);
                await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "AETC",
                    TokenName = "AElf Token Converter Token",
                    TotalSupply = 500_000L,
                    Decimals = 2,
                    Issuer = DefaultAddress,
                    IsBurnable = true,
                    LockWhiteList =
                    {
                        ProfitContractAddress,
                        TreasuryContractAddress
                    }
                });
            }

            {
                var result = AsyncHelper.RunSync(() => TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Amount = 100_000_000L,
                    To = DefaultAddress,
                    Memo = "Set for token converter."
                }));
                CheckResult(result.TransactionResult);
            }

            {
                var result = AsyncHelper.RunSync(() => TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = AliceCoinTokenInfo.Symbol,
                    Amount = AliceCoinTotalAmount - 100_000_000L,
                    To = TokenContractAddress,
                    Memo = "Set for token converter."
                }));
                CheckResult(result.TransactionResult);
            }

            {
                var result = AsyncHelper.RunSync(() => ProfitContractStub.CreateProfitItem.SendAsync(
                    new CreateProfitItemInput
                    {
                        ProfitReceivingDuePeriodCount = 10
                    }));
                CheckResult(result.TransactionResult);
            }

            {
                var result = AsyncHelper.RunSync(() =>
                    TokenConverterContractStub.Initialize.SendAsync(new InitializeInput
                    {
                        BaseTokenSymbol = DefaultSymbol,
                        FeeRate = "0.005",
                        ManagerAddress = ManagerAddress,
                        TokenContractAddress = TokenContractAddress,
                        FeeReceiverAddress = ManagerAddress,
                        Connectors = {RamConnector}
                    }));
                CheckResult(result.TransactionResult);
            }

            {
                var result =
                    AsyncHelper.RunSync(() =>
                        TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty()));
                CheckResult(result.TransactionResult);
            }
        }
        [Fact]
        public async Task Charge_Transaction_Fees()
        {

            await InitialEconomic();
            var result = (await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
            {
                SymbolToAmount = {new Dictionary<string, long> {{DefaultSymbol, 10L}}}
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 1000L,
                Memo = "transfer test",
                To = User1Address
            });
            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            balanceOutput.Balance.ShouldBe(AliceCoinTotalAmount - 1000L - 10L);
        }

        [Fact]
        public async Task Claim_Transaction_Fees()
        {
            await MultiTokenContract_Create();
            var originBalanceOutput1 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TreasuryContractAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            originBalanceOutput1.Balance.ShouldBe(0L);
            await Charge_Transaction_Fees();

            var originBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TreasuryContractAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            originBalanceOutput.Balance.ShouldBe(10L);

            {
                var result = (await TokenContractStub.ClaimTransactionFees.SendAsync(new Empty()
                )).TransactionResult;
                CheckResult(result);
            }

            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TreasuryContractAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            balanceOutput.Balance.ShouldBe(10L);

        }

        [Fact]
        public async Task Set_And_Get_Method_Fee()
        {
            await MultiTokenContract_Create();
            var feeChargerStub = GetTester<FeeChargedContractContainer.FeeChargedContractStub>(TokenContractAddress,
                DefaultKeyPair);

            // Fee not set yet.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.SymbolToAmount.Keys.ShouldNotContain(AliceCoinTokenInfo.Symbol);
            }

            // Set method fee.
            var resultSet = (await feeChargerStub.SetMethodFee.SendAsync(new SetMethodFeeInput
            {
                Method = nameof(TokenContractContainer.TokenContractStub.Transfer),
                SymbolToAmount = {new Dictionary<string, long> {{AliceCoinTokenInfo.Symbol, 10L}}}
            })).TransactionResult;
            resultSet.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check fee.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName()
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.SymbolToAmount[AliceCoinTokenInfo.Symbol].ShouldBe(10L);
            }
        }
    }
}