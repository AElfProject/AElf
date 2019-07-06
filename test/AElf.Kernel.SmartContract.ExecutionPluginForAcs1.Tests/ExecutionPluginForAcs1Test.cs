using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1.Tests
{
    public sealed class ExecutionPluginForAcs1Test : ExecutionPluginForAcs1TestBase
    {
        private TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        private Address TestContractAddress { get; set; }
        private AElf.Types.Address TokenContractAddress { get; set; }
        private TestContract.ContractContainer.ContractStub DefaultTester { get; set; }
        private ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        private Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);

        private async Task DeployContractsAsync()
        {
            {
                // TokenContract
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
                TokenContractAddress = await DeploySystemSmartContract(category, code,
                    TokenSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);
            }
            {
                var category = KernelConstants.CodeCoverageRunnerCategory;
                var code = Codes.Single(kv => kv.Key.Contains("TestContract")).Value;
                TestContractAddress = await DeployContractAsync(category, code, Hash.FromString("TestContract"), DefaultSenderKeyPair);
                DefaultTester =
                    GetTester<TestContract.ContractContainer.ContractStub>(TestContractAddress, DefaultSenderKeyPair);
            }
        }

        private async Task InitializeTokenAsync()
        {
            await TokenContractStub.CreateNativeToken.SendAsync(new CreateNativeTokenInput()
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1000_0000L,
                Issuer = DefaultSender
            });

            await TokenContractStub.Issue.SendAsync(new IssueInput()
            {
                Symbol = "ELF",
                Amount = 1000_000L,
                To = DefaultSender,
                Memo = "Set for token converter."
            });
        }

        [Fact]
        public async Task GetPreTransactionsTest()
        {
            await DeployContractsAsync();
            var plugins = Application.ServiceProvider.GetRequiredService<IEnumerable<IExecutionPlugin>>()
                .ToLookup(p => p.GetType()).Select(coll => coll.First()); // One instance per type
            var plugin = plugins.SingleOrDefault(p => p.GetType() == typeof(FeeChargeExecutionPlugin));
            plugin.ShouldNotBeNull();
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var chain = await bcs.GetChainAsync();
            var transactions = (await plugin.GetPreTransactionsAsync(TestContract.ContractContainer.Descriptors,
                new TransactionContext()
                {
                    Transaction = new Transaction()
                    {
                        From = DefaultSender,
                        To = TestContractAddress,
                        MethodName = nameof(DefaultTester.DummyMethod)
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.ShouldNotBeEmpty();
            transactions[0].From.ShouldBe(DefaultSender);
            transactions[0].To.ShouldBe(TokenContractAddress);
        }

        private async Task SetMethodFee_Successful(long feeAmount)
        {
            await DefaultTester.SetMethodFee.SendAsync(new SetMethodFeeInput()
            {
                Method = nameof(DefaultTester.DummyMethod),
                Symbol = "ELF",
                Amount = feeAmount
            });
            var fee = await DefaultTester.GetMethodFee.CallAsync(new MethodName()
            {
                Name = nameof(DefaultTester.DummyMethod)
            });
            fee.Amount.ShouldBe(feeAmount);
        }

        [Fact]
        public async Task ChargeFee_Successful()
        {
            await DeployContractsAsync();
            await InitializeTokenAsync();

            var feeAmount = 7;
            await SetMethodFee_Successful(feeAmount);

            var before = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });

            var dummy = await DefaultTester.DummyMethod.SendAsync(new Empty()); // This will deduct the fee
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var after = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });

            after.Balance.ShouldBe(before.Balance - feeAmount);
        }

        [Fact]
        public async Task ChargeFee_Prefailed()
        {
            await DeployContractsAsync();
            await InitializeTokenAsync();
            var feeAmount = 7;
            await SetMethodFee_Successful(feeAmount);
            {
                // Make sure balance insufficient
                var originalBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = DefaultSender,
                    Symbol = "ELF"
                })).Balance;
                var targetBalance = feeAmount - 1; // So that the sender doesn't have enough balance for paying the fee
                var res = await TokenContractStub.Burn.SendAsync(new BurnInput()
                {
                    Symbol = "ELF",
                    Amount = originalBalance - targetBalance
                });
                res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var before = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });
            before.Balance.ShouldBeLessThan(feeAmount);

            var dummy = await DefaultTester.DummyMethod.SendAsync(new Empty()); // This will deduct the fee
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Unexecutable);
        }
    }
}