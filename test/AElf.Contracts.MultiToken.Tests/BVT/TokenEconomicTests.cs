using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Types;
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
                    Issuer = DefaultAddress,
                    LockWhiteList = { TreasuryContractAddress }
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

        [Fact]
        public async Task Set_And_Get_Method_Fee_Test()
        {
            await MultiTokenContract_Create_Test();
            var feeChargerStub = GetTester<MethodFeeProviderContractContainer.MethodFeeProviderContractStub>(
                TokenContractAddress, DefaultKeyPair);

            // Fee not set yet.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new StringValue
                    {
                        Value = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.Fee.Select(a => a.Symbol).ShouldNotContain(AliceCoinTokenInfo.Symbol);
            }

            // Set method fee.
            var resultSet = (await feeChargerStub.SetMethodFee.SendAsync(new MethodFees
            {
                MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                Fee = {new MethodFee {Symbol = AliceCoinTokenInfo.Symbol, BasicFee = 10L}}
            })).TransactionResult;
            resultSet.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check fee.
            {
                var fee = await feeChargerStub.GetMethodFee.CallAsync(
                    new StringValue
                    {
                        Value = nameof(TokenContractContainer.TokenContractStub.Transfer)
                    });
                fee.Fee.First(a => a.Symbol == AliceCoinTokenInfo.Symbol).BasicFee.ShouldBe(10L);
            }
        }
    }
}