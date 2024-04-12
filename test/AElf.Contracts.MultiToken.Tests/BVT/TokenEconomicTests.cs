using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests
{
    private async Task InitialEconomicAsync()
    {
        {
            const long totalSupply = 100_000_00000000;
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = DefaultSymbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                Issuer = DefaultAddress,
                LockWhiteList = { TreasuryContractAddress },
                Owner = DefaultAddress
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = DefaultSymbol,
                Amount = totalSupply,
                To = DefaultAddress,
                Memo = "Set for token converter."
            });
        }
        {
            var result =
                await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
        }
        {
            var result =
                await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(
                    new Empty());
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
                    ParliamentContractAddress,
                    TreasuryContractAddress
                },
                Owner = DefaultAddress
            })).TransactionResult;
        }

        {
            AsyncHelper.RunSync(() => TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = 100_000_000L,
                To = DefaultAddress,
                Memo = "Set for token converter."
            }));
        }

        {
            AsyncHelper.RunSync(() => TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                Amount = AliceCoinTotalAmount - 100_000_000L,
                To = TokenContractAddress,
                Memo = "Set for token converter."
            }));
        }
    }
}