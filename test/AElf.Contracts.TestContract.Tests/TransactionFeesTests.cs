using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract;
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
    public class TransactionFeesTests : TestFeesContractTestBase
    {
        private const long DefaultFee = 1_00000000L;
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
            
            var netResult = await Acs8ContractStub.NetConsumingMethod.SendAsync(new NetConsumingMethodInput
            {
                Blob = GenerateBytes(1024)
            });
            CheckResult(netResult.TransactionResult);

            var stoResult = await Acs8ContractStub.StoConsumingMethod.SendAsync(new Empty());
            CheckResult(stoResult.TransactionResult);
            
            var acs8After = await GetContractResourceBalance(Acs8ContractAddress);
            
            acs8After["CPU"].ShouldBeLessThan(acs8Before["CPU"]);
            acs8After["NET"].ShouldBeLessThan(acs8Before["NET"]);
            acs8After["STO"].ShouldBeLessThan(acs8Before["STO"]);
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
            CheckResult(transactionResult.TransactionResult);

            var afterBalance = await GetBalance(DefaultSender);
            beforeBalance.ShouldBe(afterBalance + DefaultFee);
            
            var acs8After = await GetContractResourceBalance(Acs8ContractAddress);
            var feesAfter = await GetContractResourceBalance(TransactionFeesContractAddress);
            
            acs8After["CPU"].ShouldBe(acs8Before["CPU"]);
            acs8After["NET"].ShouldBe(acs8Before["NET"]);
            acs8After["STO"].ShouldBe(acs8Before["STO"]);
            
            feesAfter["CPU"].ShouldBeLessThan(feesBefore["CPU"]);
            feesAfter["NET"].ShouldBeLessThan(feesBefore["NET"]);
            feesAfter["STO"].ShouldBeLessThan(feesBefore["STO"]);
        }

        [Fact(Skip="Wait issue about execution order")]
        public async Task TransactionFee_Failed_Test()
        {
            var beforeBalance = await GetBalance(DefaultSender);
            var feesBefore = await GetContractResourceBalance(TransactionFeesContractAddress);

            var transactionResult = await TransactionFeesContractStub.FailCpuNetConsuming.SendAsync(new NetBytesInput
            {
                NetPackage = GenerateBytes(1024)
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var afterBalance = await GetBalance(DefaultSender);
            beforeBalance.ShouldBe(afterBalance + DefaultFee);
            
            var feesAfter = await GetContractResourceBalance(TransactionFeesContractAddress);
            feesAfter["CPU"].ShouldBeLessThan(feesBefore["CPU"]);
            feesAfter["NET"].ShouldBeLessThan(feesBefore["NET"]);
            feesAfter["STO"].ShouldBeLessThan(feesBefore["STO"]);
        }

        [Fact(Skip = "Wait issue about execution order")]
        public async Task TransactionFee_InlineFailed_Test()
        {
            var beforeBalance = await GetBalance(DefaultSender);
            var feesBefore = await GetContractResourceBalance(TransactionFeesContractAddress);

            var transactionResult = await TransactionFeesContractStub.FailInlineTransfer.SendAsync(
                new Contracts.TestContract.TransactionFees.TransferInput
                {
                    To = OtherTester,
                    Amount = 200_00000000,
                    Memo = "inline fail test"
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
            var afterBalance = await GetBalance(DefaultSender);
            beforeBalance.ShouldBe(afterBalance + DefaultFee);
            
            var feesAfter = await GetContractResourceBalance(TransactionFeesContractAddress);
            feesAfter["CPU"].ShouldBeLessThan(feesBefore["CPU"]);
            feesAfter["NET"].ShouldBeLessThan(feesBefore["NET"]);
            feesAfter["STO"].ShouldBeLessThan(feesBefore["STO"]);
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