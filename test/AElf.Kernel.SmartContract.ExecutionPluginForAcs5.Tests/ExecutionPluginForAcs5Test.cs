using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Acs5;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil.Cil;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests
{
    public class ExecutionPluginForAcs5Test : ExecutionPluginForAcs5TestBase
    {
        [Fact]
        public async Task GetPreTransactionsTest()
        {
            await InitializeContracts();
            await SetProfitReceivers_Successful();

            var plugins = Application.ServiceProvider.GetRequiredService<IEnumerable<IExecutionPlugin>>()
                .ToLookup(p => p.GetType()).Select(coll => coll.First());
            var plugin = plugins.SingleOrDefault(p => p.GetType() == typeof(ProfitSharingExecutionPlugin));
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
                        MethodName = nameof(DefaultTester.DummyMethod)
                    },
                    BlockHeight = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash
                })).ToList();

            transactions.ShouldNotBeEmpty();
            transactions[0].From.ShouldBe(DefaultSender);
            transactions[0].To.ShouldBe(TokenContractAddress);
        }

        [Fact]
        public async Task ChargeFee_Successful()
        {
            await InitializeContracts();
            await SetProfitReceivers_Successful();

            var before = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });

            var dummy = await DefaultTester.DummyMethod.SendAsync(new Empty());
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var after = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            });
            after.Balance.ShouldBe(before.Balance - 10);
        }

        [Fact]
        public async Task ChargeFee_PreFailed()
        {
            await InitializeContracts();
            await SetProfitReceivers_Successful();

            var originalBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            })).Balance;

            var transferAmount = originalBalance - 5;
            var burnResult = await TokenContractStub.Burn.SendAsync(new BurnInput
            {
                Symbol = "ELF",
                Amount = transferAmount
            });
            burnResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var dummy = await DefaultTester.DummyMethod.SendAsync(new Empty());
            dummy.TransactionResult.Status.ShouldBe(TransactionResultStatus.Unexecutable);
        }
        
        private async Task SetProfitReceivers_Successful()
        {
            await DefaultTester.SetMethodProfitFee.SendAsync(new SetMethodProfitFeeInput
            {
                Method = nameof(DefaultTester.DummyMethod),
                SymbolToAmount =
                {
                    { "ELF", 10 }
                }
            });

            var fee = await DefaultTester.GetMethodProfitFee.CallAsync(new StringValue
            {
                Value = nameof(DefaultTester.DummyMethod)
            });
            fee.SymbolToAmount["ELF"].ShouldBe(10);
        }
    }
}