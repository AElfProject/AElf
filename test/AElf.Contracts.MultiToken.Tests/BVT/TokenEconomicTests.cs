using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests
    {
        private async Task InitialEconomic()
        {
            {
                const long TotalSupply = 100_000_00000000;
                await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = DefaultSymbol,
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = TotalSupply,
                    Issuer = DefaultAddress
                });
                await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = DefaultSymbol,
                    Amount = TotalSupply,
                    To = DefaultAddress,
                    Memo = "Set for token converter."
                });
            }
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
                var result = (await TokenContractStub.Create.SendAsync(new CreateInput
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
        }

        [Fact(DisplayName = "[MultiToken] MultiToken_ChargeTransactionFees_Test")]
        public async Task MultiTokenContract_ChargeTransactionFees_Test()
        {
            await InitialEconomic();
            var result = (await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
            {
                SymbolToAmount = {new Dictionary<string, long> {{AliceCoinTokenInfo.Symbol, 10L}}}
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 1000L,
                Memo = "transfer test",
                To = TreasuryContractAddress
            });
            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultAddress,
                Symbol = AliceCoinTokenInfo.Symbol
            });
            balanceOutput.Balance.ShouldBe(100_000_000L - 1000L - 10L);
        }

        [Fact]
        public async Task Set_And_Get_Method_Fee_Test()
        {
            await MultiTokenContract_Create_Test();
            var feeChargerStub = GetTester<FeeChargedContractContainer.FeeChargedContractStub>(TokenContractAddress,
                DefaultKeyPair);

            // Fee not set yet.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.Amounts.Select(a => a.Symbol).ShouldNotContain(AliceCoinTokenInfo.Symbol);
            }

            // Set method fee.
            var resultSet = (await feeChargerStub.SetMethodFee.SendAsync(new TokenAmounts
            {
                Method = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Amounts = {new TokenAmount {Symbol = AliceCoinTokenInfo.Symbol, Amount = 10L}}
            })).TransactionResult;
            resultSet.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check fee.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new MethodName
                    {
                        Name = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.Amounts.First(a => a.Symbol == AliceCoinTokenInfo.Symbol).Amount.ShouldBe(10L);
            }
        }
    }
}