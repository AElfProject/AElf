using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acs8;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests.TestContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests
{
    public class ExecutionPluginForResourceFeeTest : ExecutionPluginForResourceFeeTestBase
    {
        public ExecutionPluginForResourceFeeTest()
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

            var buyResult = await TestContractStub.BuyResourceToken.SendAsync(new BuyResourceTokenInput
            {
                Symbol = "READ",
                Amount = 100_00000000,
            });
            buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task AdvanceResourceToken(List<string> except = null)
        {
            const long amount = 10_000_00000000;
            var resourceTokenList = new List<string> {"READ", "WRITE", "STORAGE", "TRAFFIC"};
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

            const string symbol = "READ";

            var balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await TestContractStub.CpuConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await TestContractStub.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
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
            await TestContractStub.FewConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await TestContractStub.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
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

            const string symbol = "STORAGE";

            var balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await TestContractStub.StoConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await TestContractStub.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
            var balanceAfter = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });

            var consumption = balanceBefore.Balance - balanceAfter.Balance;

            balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await TestContractStub.FewConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await TestContractStub.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
            balanceAfter = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });

            var lesserConsumption = balanceAfter.Balance - balanceBefore.Balance;

            consumption.ShouldBeGreaterThan(lesserConsumption);
        }

        /// <summary>
        /// TrafficConsumingMethod vs FewConsumingMethod
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task CompareTrafficTokenConsumption()
        {
            await AdvanceResourceToken();

            const string symbol = "TRAFFIC";

            var balanceBefore = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress, Symbol = symbol
            });
            await TestContractStub.TrafficConsumingMethod.SendAsync(new TrafficConsumingMethodInput
            {
                Blob = ByteString.CopyFrom("TrafficConsumingMethod vs FewConsumingMethod", Encoding.Default)
            });
            // Mine a block to use plugin to really consume resource tokens.
            await TestContractStub.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
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
            await TestContractStub.FewConsumingMethod.SendAsync(new Empty());
            // Mine a block to use plugin to really consume resource tokens.
            await TestContractStub.BuyResourceToken.SendAsync(new BuyResourceTokenInput());
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
            var txResult = await TestContractStub.CpuConsumingMethod.SendWithExceptionAsync(new Empty());
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Unexecutable);
            txResult.TransactionResult.Error.ShouldContain("is not enough");
        }

        [Fact]
        public async Task Consumption_WithoutInsufficientSTO()
        {
            await AdvanceResourceToken(new List<string> {"STORAGE"});
            const long stoAmount = 100; // Not enough.

            // Advance some STORAGE tokens.
            await TokenConverterContractStub.Buy.SendAsync(new BuyInput
            {
                Symbol = "STORAGE",
                Amount = 100_00000000L
            });
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = TestContractAddress,
                Amount = stoAmount,
                Symbol = "STORAGE"
            });

            long owingSto;

            {
                var txResult = await TestContractStub.CpuConsumingMethod.SendAsync(new Empty());
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                txResult.TransactionResult.ConsumedResourceTokens.Value["STORAGE"].ShouldBe(stoAmount);
                owingSto = txResult.TransactionResult.ConsumedResourceTokens.Owning["STORAGE"];
                owingSto.ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["READ"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["TRAFFIC"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["WRITE"].ShouldBeGreaterThan(0);
            }

            // Mine a block to use plugin to really consume resource tokens.
            await TestContractStub.BuyResourceToken.SendAsync(new BuyResourceTokenInput());

            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "STORAGE"
            })).Balance;
            balance.ShouldBe(0);

            {
                var txResult = await TestContractStub.CpuConsumingMethod.SendWithExceptionAsync(new Empty());
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Unexecutable);
                txResult.TransactionResult.Error.ShouldContain($"Owning {owingSto}");
            }

            // Advance some STORAGE tokens.
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = TestContractAddress,
                Amount = owingSto - 1, // Still not enough
                Symbol = "STORAGE"
            });

            {
                var txResult = await TestContractStub.CpuConsumingMethod.SendWithExceptionAsync(new Empty());
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Unexecutable);
                txResult.TransactionResult.Error.ShouldContain($"Owning {owingSto}");
            }

            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = TestContractAddress,
                Amount = 2, // Not it's enough
                Symbol = "STORAGE"
            });

            {
                var txResult = await TestContractStub.CpuConsumingMethod.SendAsync(new Empty());
                txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                txResult.TransactionResult.ConsumedResourceTokens.Value["STORAGE"].ShouldBe(owingSto + 1);
                txResult.TransactionResult.ConsumedResourceTokens.Owning["STORAGE"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["READ"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["TRAFFIC"].ShouldBeGreaterThan(0);
                txResult.TransactionResult.ConsumedResourceTokens.Value["WRITE"].ShouldBeGreaterThan(0);
            }
        }

        [Fact]
        public async Task CompareConsumptions()
        {
            await AdvanceResourceToken();

            var (read, write, traffic, txResult1) =
                await GetTransactionResourcesCost(TestContractStub.CpuConsumingMethod.SendAsync);
            var (read1, write1, traffic1, txResult2) =
                await GetTransactionResourcesCost(TestContractStub.FewConsumingMethod.SendAsync);

            read.ShouldBeGreaterThan(read1);
            write.ShouldBeGreaterThan(write1);
            traffic.ShouldBe(traffic1);

            txResult1.ConsumedResourceTokens.IsFailedToCharge.ShouldBe(false);
            txResult2.ConsumedResourceTokens.IsFailedToCharge.ShouldBe(false);
            txResult1.ConsumedResourceTokens.Value["READ"]
                .ShouldBeGreaterThan(txResult2.ConsumedResourceTokens.Value["READ"]);
            txResult1.ConsumedResourceTokens.Value["WRITE"]
                .ShouldBeGreaterThan(txResult2.ConsumedResourceTokens.Value["WRITE"]);
            txResult1.ConsumedResourceTokens.Value["TRAFFIC"]
                .ShouldBe(txResult2.ConsumedResourceTokens.Value["TRAFFIC"]);
            txResult1.ConsumedResourceTokens.Value["STORAGE"]
                .ShouldBe(txResult2.ConsumedResourceTokens.Value["STORAGE"]);
        }

        private async Task<(long read, long write, long net, TransactionResult txResult)> GetTransactionResourcesCost(
            Func<Empty, Task<IExecutionResult<Empty>>> action)
        {
            var beforeRead = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "READ"
            })).Balance;
            var beforeWrite = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "WRITE"
            })).Balance;
            var beforeNet = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "TRAFFIC"
            })).Balance;

            var txResult = (await action(new Empty())).TransactionResult;
            // Mine a block to use plugin to really consume resource tokens.
            await TestContractStub.BuyResourceToken.SendAsync(new BuyResourceTokenInput());

            var afterRead = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "READ"
            })).Balance;
            var afterWrite = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "WRITE"
            })).Balance;
            var afterNet = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TestContractAddress,
                Symbol = "TRAFFIC"
            })).Balance;

            return (beforeRead - afterRead, beforeWrite - afterWrite, beforeNet - afterNet, txResult);
        }
    }
}