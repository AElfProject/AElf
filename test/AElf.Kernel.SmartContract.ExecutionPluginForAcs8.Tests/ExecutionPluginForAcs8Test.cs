using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acs8;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract;
using AElf.Sdk.CSharp;
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

            var approveResult = await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = "ELF",
                Amount = beforeElf / 2,
                To = TestContractAddress
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var buyResult = await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput
            {
                Symbol = "CPU",
                Amount = 100_00000000,
            });
            buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task AdvanceResourceToken(List<string> except = null)
        {
            const long amount = 10_000_00000000;
            var resourceTokenList = new List<string> {"CPU", "STO", "NET", "RAM"};
            if (except != null && except.Any())
            {
                resourceTokenList = resourceTokenList.Except(except).ToList();
            }

            foreach (var symbol in resourceTokenList)
            {
                await TokenConverterContractStub.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = amount
                });
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
            var txResult = await DefaultTester.CpuConsumingMethod.SendWithExceptionAsync(new Empty());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Unexecutable);
            txResult.TransactionResult.Error.ShouldContain("is not enough");
        }

        [Fact]
        public async Task Consumption_WithoutInsufficientSTO()
        {
            await AdvanceResourceToken(new List<string> {"STO"});
            const long stoAmount = 100; // Not enough.

            // Advance some STO tokens.
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = TestContractAddress,
                Amount = stoAmount,
                Symbol = "STO"
            });

            long owingSto;

            {
                var txResult = await DefaultTester.CpuConsumingMethod.SendAsync(new Empty());
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                txResult.TransactionResult.ConsumedResourceTokens.Value["STO"].ShouldBe(stoAmount);
                owingSto = txResult.TransactionResult.ConsumedResourceTokens.Owning["STO"];
                owingSto.ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["CPU"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["NET"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["RAM"].ShouldBeGreaterThan(0);
            }

            // Mine a block to use plugin to really consume resource tokens.
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput());

            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "STO"
            })).Balance;
            balance.ShouldBe(0);

            {
                var txResult = await DefaultTester.CpuConsumingMethod.SendWithExceptionAsync(new Empty());
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Unexecutable);
                txResult.TransactionResult.Error.ShouldContain($"Owning {owingSto}");
            }

            // Advance some STO tokens.
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = TestContractAddress,
                Amount = owingSto - 1, // Still not enough
                Symbol = "STO"
            });

            {
                var txResult = await DefaultTester.CpuConsumingMethod.SendWithExceptionAsync(new Empty());
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Unexecutable);
                txResult.TransactionResult.Error.ShouldContain($"Owning {owingSto}");
            }

            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = TestContractAddress,
                Amount = 2, // Not it's enough
                Symbol = "STO"
            });

            {
                var txResult = await DefaultTester.CpuConsumingMethod.SendAsync(new Empty());
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                txResult.TransactionResult.ConsumedResourceTokens.Value["STO"].ShouldBe(owingSto + 1);
                txResult.TransactionResult.ConsumedResourceTokens.Owning["STO"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["CPU"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["NET"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["RAM"].ShouldBeGreaterThan(0);
            }
        }

        [Fact]
        public async Task CompareConsumptions()
        {
            await AdvanceResourceToken();

            var (cpu, ram, net, txResult1) =
                await GetTransactionResourcesCost(DefaultTester.CpuConsumingMethod.SendAsync);
            var (cpu1, ram1, net1, txResult2) =
                await GetTransactionResourcesCost(DefaultTester.FewConsumingMethod.SendAsync);

            cpu.ShouldBeGreaterThan(cpu1);
            ram.ShouldBeGreaterThan(ram1);
            net.ShouldBe(net1);

            txResult1.ConsumedResourceTokens.IsFailedToCharge.ShouldBe(false);
            txResult2.ConsumedResourceTokens.IsFailedToCharge.ShouldBe(false);
            txResult1.ConsumedResourceTokens.Value["CPU"]
                .ShouldBeGreaterThan(txResult2.ConsumedResourceTokens.Value["CPU"]);
            txResult1.ConsumedResourceTokens.Value["RAM"]
                .ShouldBeGreaterThan(txResult2.ConsumedResourceTokens.Value["RAM"]);
            txResult1.ConsumedResourceTokens.Value["NET"]
                .ShouldBe(txResult2.ConsumedResourceTokens.Value["NET"]);
            txResult1.ConsumedResourceTokens.Value["STO"]
                .ShouldBe(txResult2.ConsumedResourceTokens.Value["STO"]);
        }

        private async Task<(long cpu, long ram, long net, TransactionResult txResult)> GetTransactionResourcesCost(
            Func<Empty, Task<IExecutionResult<Empty>>> action)
        {
            var beforeCpu = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "CPU"
            })).Balance;
            var beforeRam = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "RAM"
            })).Balance;
            var beforeNet = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "NET"
            })).Balance;

            var txResult = (await action(new Empty())).TransactionResult;
            // Mine a block to use plugin to really consume resource tokens.
            await DefaultTester.BuyResourceToken.SendAsync(new BuyResourceTokenInput());

            var afterCpu = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "CPU"
            })).Balance;
            var afterRam = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "RAM"
            })).Balance;
            var afterNet = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "NET"
            })).Balance;

            return (beforeCpu - afterCpu, beforeRam - afterRam, beforeNet - afterNet, txResult);
        }
    }
}