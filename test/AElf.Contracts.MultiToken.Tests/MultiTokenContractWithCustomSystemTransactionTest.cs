using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken;

public class MultiTokenContractWithCustomSystemTransactionTest : MultiTokenContractTestBase
{
    private static readonly long _totalSupply = 1_000_000_00000000L;

    public MultiTokenContractWithCustomSystemTransactionTest()
    {
        AsyncHelper.RunSync(async () => await InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        await TokenContractStub.SetPrimaryTokenSymbol.SendAsync(new SetPrimaryTokenSymbolInput
        {
            Symbol = DefaultSymbol
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Symbol = DefaultSymbol,
            Amount = _totalSupply,
            To = DefaultAddress,
            Memo = "Set for token converter."
        });
        var tokenSymbol = await TokenContractStub.GetPrimaryTokenSymbol.CallAsync(new Empty());
        tokenSymbol.Value.ShouldBe(DefaultSymbol);
    }

    [Fact]
    public async Task TokenContract_WithSystemTransaction_Test()
    {
        var transferAmountInSystemTxn = 1000L;
        // Set the address so that a transfer
        var generator = Application.ServiceProvider.GetRequiredService<TestTokenBalanceTransactionGenerator>();
        generator.GenerateTransactionFunc = (_, preBlockHeight, preBlockHash) =>
            new Transaction
            {
                From = DefaultAddress,
                To = TokenContractAddress,
                MethodName = nameof(TokenContractImplContainer.TokenContractImplStub.Transfer),
                Params = new TransferInput
                {
                    Amount = transferAmountInSystemTxn,
                    Memo = "transfer test",
                    Symbol = DefaultSymbol,
                    To = Accounts[1].Address
                }.ToByteString(),
                RefBlockNumber = preBlockHeight,
                RefBlockPrefix = BlockHelper.GetRefBlockPrefix(preBlockHash)
            };
        var result = await TokenContractStub.GetBalance.SendAsync(new GetBalanceInput
        {
            Symbol = DefaultSymbol,
            Owner = DefaultAddress
        });

        result.Output.Balance.ShouldBe(_totalSupply - transferAmountInSystemTxn);
    }
}