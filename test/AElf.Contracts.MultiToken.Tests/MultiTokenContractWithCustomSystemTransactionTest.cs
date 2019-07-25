using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractWithCustomSystemTransactionTest : MultiTokenContractTestBase
    {
        private static long _totalSupply = 1_000_000L;

        public MultiTokenContractWithCustomSystemTransactionTest()
        {
            AsyncHelper.RunSync(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            {
                // TokenContract
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = TokenContractCode;
                TokenContractAddress = await DeployContractAsync(category, code, Hash.FromString("MultiToken"), DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);

                await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput()
                {
                    Symbol = DefaultSymbol,
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = _totalSupply,
                    Issuer = DefaultSender
                });
                await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = DefaultSymbol,
                    Amount = _totalSupply,
                    To = DefaultSender,
                    Memo = "Set for token converter."
                });
            }
        }

        [Fact]
        public async Task TokenContract_WithSystemTransaction()
        {
            var transferAmountInSystemTxn = 1000L;
            // Set the address so that a transfer
            var generator = Application.ServiceProvider.GetRequiredService<TestTokenBalanceTransactionGenerator>();
            generator.GenerateTransactionFunc = (_, preBlockHeight, preBlockHash) =>
                new Transaction
                {
                    From = DefaultSender,
                    To = TokenContractAddress,
                    MethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                    Params = new TransferInput
                    {
                        Amount = transferAmountInSystemTxn,
                        Memo = "transfer test",
                        Symbol = DefaultSymbol,
                        To = SampleAddress.AddressList[0]
                    }.ToByteString(),
                    RefBlockNumber = preBlockHeight,
                    RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray())
                };
            var result = await TokenContractStub.GetBalance.SendAsync(new GetBalanceInput
            {
                Symbol = DefaultSymbol,
                Owner = DefaultSender
            });
            generator.GenerateTransactionFunc = null;
            result.Output.Balance.ShouldBe(_totalSupply - transferAmountInSystemTxn);
        }
    }
}