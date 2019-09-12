using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Acs8;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests
{
    public class ExecutionPluginForAcs8Test : ExecutionPluginForAcs8TestBase
    {
        public ExecutionPluginForAcs8Test()
        {
            AsyncHelper.RunSync(InitializeContracts);
        }

        [Fact]
        public async Task BuyResourceToken_Test()
        {
            var beforeElf = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = DefaultSender,
                Symbol = "ELF"
            })).Balance;
            
            var approveResult = await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = "ELF",
                Amount = beforeElf,
                Spender = TokenConverterAddress
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput
            {
                Symbol = "CPU",
                Amount = 100_00000000,
            });
        }

        [Fact]
        public async Task AdvanceResourceToken()
        {
            const long amount = 10_000_00000000;
            var resourceTokenList = new List<string> {"CPU", "STO", "NET"};
            foreach (var symbol in resourceTokenList)
            {
                await TokenContractStub.Transfer.SendAsync(new TransferInput
                {
                    To = TestContractAddress,
                    Amount = amount,
                    Symbol = symbol
                });
            }

            foreach (var symbol in resourceTokenList)
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = TestContractAddress,
                    Symbol = symbol
                })).Balance;
                balance.ShouldBe(amount);
            }
        }

        /// <summary>
        /// CpuConsumingMethod vs FewConsumingMethod
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CompareCpuTokenConsumption()
        {
            await AdvanceResourceToken();

            const string symbol = "CPU";

            var balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await DefaultTester.CpuConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
            var balanceAfter = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            var consumption = balanceBefore.Balance - balanceAfter.Balance;
            consumption.ShouldBeGreaterThan(0);

            balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await DefaultTester.FewConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
            balanceAfter = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });

            var lesserConsumption = balanceBefore.Balance - balanceAfter.Balance;

            consumption.ShouldBeGreaterThan(lesserConsumption);
        }

        /// <summary>
        /// StoConsumingMethod vs FewConsumingMethod
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CompareStoTokenConsumption()
        {
            await AdvanceResourceToken();

            const string symbol = "STO";

            var balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await DefaultTester.StoConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
            var balanceAfter = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });

            var consumption = balanceBefore.Balance - balanceAfter.Balance;

            balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await DefaultTester.FewConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
            balanceAfter = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });

            var lesserConsumption = balanceAfter.Balance - balanceBefore.Balance;

            consumption.ShouldBeGreaterThan(lesserConsumption);
        }

        /// <summary>
        /// NetConsumingMethod vs FewConsumingMethod
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CompareNetTokenConsumption()
        {
            await AdvanceResourceToken();

            const string symbol = "NET";

            var balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await DefaultTester.NetConsumingMethod.SendAsync(new NetConsumingMethodInput
            {
                Blob = ByteString.CopyFrom("NetConsumingMethod vs FewConsumingMethod", Encoding.Default)
            });
            // Mine a block to use plugin to really consume resource tokens.
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
            var balanceAfter = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });

            var consumption = balanceBefore.Balance - balanceAfter.Balance;

            consumption.ShouldBeGreaterThan(0);

            balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await DefaultTester.FewConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
            balanceAfter = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });

            var lesserConsumption = balanceBefore.Balance - balanceAfter.Balance;

            consumption.ShouldBeGreaterThan(lesserConsumption);
        }

        [Fact]
        public async Task CompareCpuTokenConsumption_WithoutResource()
        {
            var cpuConsumingResult = await DefaultTester.CpuConsumingMethod.SendAsync(new Empty());
            cpuConsumingResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Unexecutable);
            cpuConsumingResult.TransactionResult.Error.ShouldContain("CPU token is not enough");
        }

        [Fact]
        public async Task CompareConsumptions()
        {
            await AdvanceResourceToken();

            var (cpu, sto, net) =
                await GetTransactionResourcesCost(DefaultTester.CpuConsumingMethod.SendAsync);
            var (cpu1, sto1, net1) = 
                await GetTransactionResourcesCost(DefaultTester.FewConsumingMethod.SendAsync);
            
            cpu.ShouldBeGreaterThan(cpu1);
            sto.ShouldBeGreaterThan(sto1);
            net.ShouldBe(net1);
        }

        private async Task<(long cpu, long sto, long net)> GetTransactionResourcesCost(Func<Empty, Task<IExecutionResult<Empty>>> action)
        {
            var beforeCpu = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "CPU"
            })).Balance;
            var beforeSto = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "STO"
            })).Balance;
            var beforeNet = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "NET"
            })).Balance;

            await action(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput());

            var afterCpu = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "CPU"
            })).Balance;
            var afterSto = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "STO"
            })).Balance;
            var afterNet = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "NET"
            })).Balance;

            return (beforeCpu - afterCpu, beforeSto - afterSto, beforeNet - afterNet);
        }
    }
}