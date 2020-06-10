using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests.TestContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
using Xunit.Abstractions;
using TransferInput = AElf.Contracts.MultiToken.TransferInput;

namespace AElf.Contract.TestContract
{
    // TODO: Need to be refactored by new framework.
    public class TransactionFeesTests : TestFeesContractTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        public TransactionFeesTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            AsyncHelper.RunSync(DeployTestContracts);
            AsyncHelper.RunSync(InitializeTestContracts);
            AsyncHelper.RunSync(PrepareResourcesToContract);
        }

        [Fact]
        public async Task Acs8_Mined_Test()
        {
            var acs8Before = await GetContractResourceBalance(Acs8ContractAddress);
            
            var cpuResult = await Acs8ContractStub.CpuConsumingMethod.SendAsync(new Empty());
            CheckResult(cpuResult.TransactionResult);
            
            var netResult = await Acs8ContractStub.TrafficConsumingMethod.SendAsync(new TrafficConsumingMethodInput
            {
                Blob = GenerateBytes(1024)
            });
            CheckResult(netResult.TransactionResult);

            var stoResult = await Acs8ContractStub.StoConsumingMethod.SendAsync(new Empty());
            CheckResult(stoResult.TransactionResult);
            
            var acs8After = await GetContractResourceBalance(Acs8ContractAddress);
            
            acs8After["READ"].ShouldBeLessThan(acs8Before["READ"]);
            acs8After["TRAFFIC"].ShouldBeLessThan(acs8Before["TRAFFIC"]);
            acs8After["STORAGE"].ShouldBeLessThan(acs8Before["STORAGE"]);
        }
        
        [Fact]
        public async Task TransactionFee_Mined_Test()
        {
            var beforeBalance = await GetBalance(DefaultSender);
            var acs8Before = await GetContractResourceBalance(Acs8ContractAddress);
            var feesBefore = await GetContractResourceBalance(TransactionFeesContractAddress);
            
            var transactionResult = await TransactionFeesContractStub.MessCpuNetConsuming.SendAsync(new NetBytesInput
            {
                NetPackage = GenerateBytes(1024)
            });
            var transactionSize = transactionResult.Transaction.Size();
            CheckResult(transactionResult.TransactionResult);

            var acs8After = await GetContractResourceBalance(Acs8ContractAddress);
            var feesAfter = await GetContractResourceBalance(TransactionFeesContractAddress);
            
            acs8After["READ"].ShouldBe(acs8Before["READ"]);
            acs8After["TRAFFIC"].ShouldBe(acs8Before["TRAFFIC"]);
            acs8After["STORAGE"].ShouldBe(acs8Before["STORAGE"]);
            
            feesAfter["READ"].ShouldBeLessThan(feesBefore["READ"]);
            feesAfter["TRAFFIC"].ShouldBeLessThan(feesBefore["TRAFFIC"]);
            feesAfter["STORAGE"].ShouldBeLessThan(feesBefore["STORAGE"]);
        }

        [Fact]
        public async Task TransactionFee_Failed_Test()
        {
            var beforeBalance = await GetBalance(DefaultSender);
            var feesBefore = await GetContractResourceBalance(TransactionFeesContractAddress);

            var transactionResult = await TransactionFeesContractStub.FailCpuNetConsuming.SendWithExceptionAsync(new NetBytesInput
            {
                NetPackage = GenerateBytes(1024)
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            var relatedLog = transactionResult.TransactionResult.Logs.FirstOrDefault(l => l.Name == nameof(TransactionFeeCharged));
            var txFee = relatedLog == null ? 0 : TransactionFeeCharged.Parser.ParseFrom(relatedLog.NonIndexed).Amount;
            var afterBalance = await GetBalance(DefaultSender);
            beforeBalance.ShouldBe(afterBalance + txFee);

            var feesAfter = await GetContractResourceBalance(TransactionFeesContractAddress);
            feesAfter["READ"].ShouldBeLessThan(feesBefore["READ"]);
            feesAfter["TRAFFIC"].ShouldBeLessThan(feesBefore["TRAFFIC"]);
            feesAfter["STORAGE"].ShouldBeLessThan(feesBefore["STORAGE"]);
        }

        [Fact]
        public async Task TransactionFee_InlineFailed_Test()
        {
            var beforeBalance = await GetBalance(DefaultSender);
            var feesBefore = await GetContractResourceBalance(TransactionFeesContractAddress);

            var transactionResult = await TransactionFeesContractStub.FailInlineTransfer.SendWithExceptionAsync(
                new Contracts.TestContract.TransactionFees.TransferInput
                {
                    To = OtherTester,
                    Amount = 200_00000000,
                    Memo = "inline fail test"
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            var feesAfter = await GetContractResourceBalance(TransactionFeesContractAddress);
            feesAfter["READ"].ShouldBeLessThan(feesBefore["READ"]);
            feesAfter["TRAFFIC"].ShouldBeLessThan(feesBefore["TRAFFIC"]);
            feesAfter["STORAGE"].ShouldBeLessThan(feesBefore["STORAGE"]);
        }

        private async Task PrepareResourcesToContract()
        {
            foreach (var symbol in ResourceTokenSymbols)
            {
                //buy resources
                var buyResult = await TokenConverterContractStub.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = 1_0000_00000000,
                    PayLimit = 0
                });
                CheckResult(buyResult.TransactionResult);

                //transfer to acs8
                var transferResult = await TokenContractStub.Transfer.SendAsync(new TransferInput
                {
                    Symbol = symbol,
                    To = Acs8ContractAddress,
                    Amount = 4000_00000000L,
                    Memo = $"transfer {symbol} to acs8"
                });
                CheckResult(transferResult.TransactionResult);
                var acs8ContractBalance = await GetBalance(Acs8ContractAddress, symbol);
                acs8ContractBalance.ShouldBe(4000_00000000L);
                
                //transfer to transaction fees
                transferResult = await TokenContractStub.Transfer.SendAsync(new TransferInput
                {
                    Symbol = symbol,
                    To = TransactionFeesContractAddress,
                    Amount = 4000_00000000L,
                    Memo = $"transfer {symbol} to transaction fees"
                });
                CheckResult(transferResult.TransactionResult);
                var feeContractBalance = await GetBalance(TransactionFeesContractAddress, symbol);
                feeContractBalance.ShouldBe(4000_00000000L);
            }

            //transfer some token to acs8
            var transactionResult = await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Amount = 1000_00000000,
                Symbol = "ELF", 
                To = Acs8ContractAddress,
                Memo = "for execution size fee"
            });
            
            //initialize transactions fee contract
            var initializeResult = await TransactionFeesContractStub.InitializeFeesContract.SendAsync(Acs8ContractAddress);
            CheckResult(initializeResult.TransactionResult);
        }

        private async Task<long> GetBalance(Address owner, string symbol = "ELF")
        {
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = owner,
                Symbol = symbol
            });

            return balance.Balance;
        }

        private async Task<Dictionary<string, long>> GetContractResourceBalance(Address contract)
        {
            var dic = new Dictionary<string, long>();
            foreach (var symbol in ResourceTokenSymbols)
            {
                var balance = await GetBalance(contract, symbol);
                dic[symbol] = balance;
                _testOutputHelper.WriteLine($"Contract: {contract}, Symbol: {symbol}, Balance: {balance}");
            }

            return dic;
        }

        private ByteString GenerateBytes(int length)
        {
            var rd = new Random(DateTime.Now.Millisecond);
            var bytes = new byte[length];
            rd.NextBytes(bytes);

            return ByteString.CopyFrom(bytes);
        }
    }
}