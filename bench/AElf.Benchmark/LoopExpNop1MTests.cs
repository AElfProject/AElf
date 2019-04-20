using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Benchmark.PerformanceTestContract;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using BenchmarkDotNet.Attributes;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Benchmark
{
    public class LoopExpNop1MTests : BenchmarkTestBase
    {
        private IBlockchainService _blockchainService;
        private ISmartContractAddressService _smartContractAddressService;
        private IAccountService _accountService;
        private ITransactionResultService _transactionResultService;
        private ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private OSTestHelper _osTestHelper;

        private Transaction _transaction;
        private Block _block;
        private Address _contractAddress;
        private Chain _chain;
        private TransactionTrace _transactionTrace;
        
        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _accountService = GetRequiredService<IAccountService>();
            _transactionResultService = GetRequiredService<ITransactionResultService>();
            _transactionReadOnlyExecutionService = GetRequiredService<ITransactionReadOnlyExecutionService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();

            var basicContractZero = _smartContractAddressService.GetZeroSmartContractAddress();

            var transaction = _osTestHelper.GenerateTransaction(Address.Generate(), basicContractZero,
                nameof(ISmartContractZero.DeploySmartContract), new ContractDeploymentInput()
                {
                    Category = 0,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(PerformanceTestContract.PerformanceTestContract)
                        .Assembly.Location))
                });

            var signature = await _accountService.SignAsync(transaction.GetHash().DumpByteArray());
            transaction.Sigs.Add(ByteString.CopyFrom(signature));

            await _osTestHelper.BroadcastTransactions(new List<Transaction> {transaction});
            await _osTestHelper.MinedOneBlock();

            var txResult = await _transactionResultService.GetTransactionResultAsync(transaction.GetHash());

            _contractAddress = Address.Parser.ParseFrom(txResult.ReturnValue);
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            _chain = await _blockchainService.GetChainAsync();

            _transaction = _osTestHelper.GenerateTransaction(Address.Generate(), _contractAddress,
                "Nop", new PerformanceTesteInput()
                {
                    Exponent = 0,
                    Seed = 15,
                    N = 1000000
                });
        }

        [Benchmark]
        public async Task LoopExpNop1M()
        {
            _transactionTrace = await _transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = _chain.BestChainHash,
                    BlockHeight = _chain.BestChainHeight
                },
                _transaction,
                DateTime.UtcNow);
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            var calResult = UInt64Value.Parser.ParseFrom(_transactionTrace.ReturnValue).Value;
            if (calResult != 15)
            {
                throw new Exception("execute fail");
            }
        }
    }
}