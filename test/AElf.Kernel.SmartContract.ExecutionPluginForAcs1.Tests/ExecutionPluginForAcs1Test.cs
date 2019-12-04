using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using AElf.Contracts.MultiToken;
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
        private Address TokenContractAddress { get; set; }
        private TestContract.ContractContainer.ContractStub TestContractStub { get; set; }
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
                TestContractAddress = await DeploySystemSmartContract(category, code, Hash.FromString("TestContract"),
                    DefaultSenderKeyPair);
                TestContractStub =
                    GetTester<TestContract.ContractContainer.ContractStub>(TestContractAddress, DefaultSenderKeyPair);
            }
        }

        private async Task CreateAndIssueTokenAsync(string symbol = "ELF", long issueAmount = 1000_00000000L)
        {
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1_000_000_00000000L,
                Issuer = DefaultSender
            });

            if (issueAmount != 0)
            {
                await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = symbol,
                    Amount = issueAmount,
                    To = DefaultSender,
                    Memo = "Set for token converter."
                });
            }
        }

        [Fact]
        public async Task GetPreTransactionsTest()
        {
            await DeployContractsAsync();
            await CreateAndIssueTokenAsync();

            await SetMethodFee_Successful(10);
            var plugins = Application.ServiceProvider.GetRequiredService<IEnumerable<IPreExecutionPlugin>>()
                .ToLookup(p => p.GetType()).Select(coll => coll.First()); // One instance per type
            var plugin = plugins.SingleOrDefault(p => p.GetType() == typeof(FeeChargePreExecutionPlugin));
            plugin.ShouldNotBeNull();
            var bcs = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var chain = await bcs.GetChainAsync();
            var transactions = (await plugin.GetPreTransactionsAsync(TestContract.ContractContainer.Descriptors,
                new TransactionContext
                {
                    Transaction = new Transaction
                    {
                        From = DefaultSender,
                        To = TestContractAddress,
                        MethodName = nameof(TestContractStub.DummyMethod)
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
            await TestContractStub.SetMethodFee.SendAsync(new MethodFees
            {
                MethodName = nameof(TestContractStub.DummyMethod),
                Fees =
                {
                    new MethodFee {Symbol = "ELF", BasicFee = feeAmount}
                }
            });
            var fee = await TestContractStub.GetMethodFee.CallAsync(new StringValue
            {
                Value = nameof(TestContractStub.DummyMethod)
            });
            fee.Fees.First(a => a.Symbol == "ELF").BasicFee.ShouldBe(feeAmount);
        }

        [Fact]
        public async Task ChargeFee_SuccessfulTest()
        {
            await DeployContractsAsync();
            await CreateAndIssueTokenAsync();

            var feeAmount = 7;
            await SetMethodFee_Successful(feeAmount);

            var before = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });

            var dummy = await TestContractStub.DummyMethod.SendAsync(new Empty()); // This will deduct the fee
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var size = dummy.Transaction.Size();
            var calculator = Application.ServiceProvider.GetRequiredService<ICalculateFeeService>();
            var sizeFee = calculator.CalculateFee(FeeType.Tx, size);
            dummy.TransactionResult.TransactionFee.Value["ELF"].ShouldBe(feeAmount + sizeFee);
            var after = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });

            after.Balance.ShouldBe(before.Balance - feeAmount - sizeFee);
        }

        [Fact]
        public async Task ChargeFee_SizeFee_FailedTest()
        {
            await DeployContractsAsync();
            await CreateAndIssueTokenAsync();

            var feeAmount = 100000;
            await SetMethodFee_Successful(feeAmount);

            // Make sure balance insufficient
            var originalBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            })).Balance;
            var targetBalance = 2000000000; // So that the sender doesn't have enough balance for paying the fee
            var res = await TokenContractStub.Burn.SendAsync(new BurnInput()
            {
                Symbol = "ELF",
                Amount = originalBalance - targetBalance
            });
            res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var dummy = await TestContractStub.DummyMethod
                .SendWithExceptionAsync(new Empty()); // This will deduct the fee
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            dummy.TransactionResult.Error.ShouldBe(ExecutionStatus.InsufficientTransactionFees.ToString());

            var afterFee = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            })).Balance;
            afterFee.ShouldBe(0);
        }

        [Fact]
        public async Task ChargeFee_TransactionFee_FailedTest()
        {
            await DeployContractsAsync();
            await CreateAndIssueTokenAsync();

            var feeAmount = 100000;
            await SetMethodFee_Successful(feeAmount);

            await SetMethodFee_Successful(feeAmount);

            {
                // Make sure balance insufficient
                var originalBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = DefaultSender,
                    Symbol = "ELF"
                })).Balance;
                var targetBalance = 1000000000; // So that the sender doesn't have enough balance for paying the fee
                var res = await TokenContractStub.Burn.SendAsync(new BurnInput()
                {
                    Symbol = "ELF",
                    Amount = originalBalance - targetBalance
                });
                res.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var dummyResult = await TestContractStub.DummyMethod.SendWithExceptionAsync(new Empty());
            dummyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            dummyResult.TransactionResult.Error.ShouldBe(ExecutionStatus.InsufficientTransactionFees.ToString());

            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });
            balance.Balance.ShouldBe(0);
        }

        [Theory]
        [InlineData(9, 0, 3, 10, 1, 2, "TSB", 2, true)]
        [InlineData(9, 0, 1, 10, 1, 2, "ELF", 9, false)]
        [InlineData(1, 2, 2, 0, 1, 2, "TSA", 1, true)]
        [InlineData(1, 0, 1, 0, 1, 2, "TSB", 1, false)]
        [InlineData(10, 0, 0, 0, 1, 2, null, 0, false)]
        public async Task ChargeFeeFailedTests(long balance1, long balance2, long balance3, long fee1, long fee2, long fee3,
            string chargedSymbol, long chargedAmount, bool isChargingSuccessful)
        {
            await DeployContractsAsync();

            await CreateAndIssueTokenAsync("ELF", balance1);
            await CreateAndIssueTokenAsync("TSA", balance2);
            await CreateAndIssueTokenAsync("TSB", balance3);

            var methodFee = new MethodFees
            {
                MethodName = nameof(TestContractStub.DummyMethod)
            };
            if (fee1 > 0)
                methodFee.Fees.Add(new MethodFee {Symbol = "ELF", BasicFee = fee1});
            if (fee2 > 0)
                methodFee.Fees.Add(new MethodFee {Symbol = "TSA", BasicFee = fee2});
            if (fee3 > 0)
                methodFee.Fees.Add(new MethodFee {Symbol = "TSB", BasicFee = fee3});
            await TestContractStub.SetMethodFee.SendAsync(methodFee);

            var originBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultSender,
                Symbol = chargedSymbol ?? "ELF"
            })).Balance;

            if (isChargingSuccessful)
            {
                var dummyResult = await TestContractStub.DummyMethod.SendAsync(new Empty());
                dummyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                if (chargedSymbol != null)
                {
                    dummyResult.TransactionResult.TransactionFee.Value.Keys.First().ShouldBe(chargedSymbol);
                    dummyResult.TransactionResult.TransactionFee.Value.Values.First().ShouldBe(chargedAmount);
                }
            }
            else
            {
                var dummyResult = await TestContractStub.DummyMethod.SendWithExceptionAsync(new Empty());
                dummyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                dummyResult.TransactionResult.Error.ShouldBe(ExecutionStatus.InsufficientTransactionFees.ToString());
                if (chargedSymbol != null)
                {
                    dummyResult.TransactionResult.TransactionFee.Value.Keys.First().ShouldBe(chargedSymbol);
                    dummyResult.TransactionResult.TransactionFee.Value.Values.First().ShouldBeGreaterThan(0);
                }
            }

            var finalBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultSender,
                Symbol = chargedSymbol ?? "ELF"
            })).Balance;

            (originBalance - finalBalance).ShouldBe(chargedAmount);
        }
    }
}