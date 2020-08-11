using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.ContractTestBase.ContractTestKit
{
    public class TestTransactionExecutor : ITestTransactionExecutor
    {
        private readonly IServiceProvider _serviceProvider;

        public TestTransactionExecutor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TransactionResult> ExecuteAsync(Transaction transaction)
        {
            var transactionResult = await ExecuteTransactionAsync(transaction);
            if (transactionResult == null || transactionResult.Status != TransactionResultStatus.Mined)
                throw new Exception($"Failed to execute {transaction.MethodName}. {transactionResult?.Error}");
            return transactionResult;
        }

        public async Task<TransactionResult> ExecuteWithExceptionAsync(Transaction transaction)
        {
            var transactionResult = await ExecuteTransactionAsync(transaction);
            if (transactionResult.Status == TransactionResultStatus.Mined)
            {
                throw new Exception($"Succeed to execute {transaction.MethodName}.");
            }

            return transactionResult;
        }

        private async Task<TransactionResult> ExecuteTransactionAsync(Transaction transaction)
        {
            var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var miningService = _serviceProvider.GetRequiredService<IMiningService>();
            var blockAttachService = _serviceProvider.GetRequiredService<IBlockAttachService>();
            var blockTimeProvider = _serviceProvider.GetRequiredService<IBlockTimeProvider>();
            
            var blockExecutedSet = await miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = preBlock.GetHash(), PreviousBlockHeight = preBlock.Height,
                    BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(int.MaxValue),
                    TransactionCountLimit = int.MaxValue
                },
                new List<Transaction> {transaction},
                blockTimeProvider.GetBlockTime());

            var block = blockExecutedSet.Block;

            await blockchainService.AddTransactionsAsync(new List<Transaction> {transaction});
            await blockchainService.AddBlockAsync(block);
            await blockAttachService.AttachBlockAsync(block);

            return blockExecutedSet.TransactionResultMap[transaction.GetHash()];
        }

        public async Task<ByteString> ReadAsync(Transaction transaction)
        {
            var transactionTrace = await ReadTransactionResultAsync(transaction);
            if (transactionTrace.ExecutionStatus != ExecutionStatus.Executed)
                throw new Exception($"Failed to call {transaction.MethodName}. {transactionTrace.Error}");
            return transactionTrace.ReturnValue;
        }

        public async Task<StringValue> ReadWithExceptionAsync(Transaction transaction)
        {
            var transactionTrace = await ReadTransactionResultAsync(transaction);
            if (transactionTrace.ExecutionStatus == ExecutionStatus.Executed)
            {
                throw new Exception($"Succeed to call {transaction.MethodName}.");
            }

            return new StringValue {Value = transactionTrace.Error};
        }

        private async Task<TransactionTrace> ReadTransactionResultAsync(Transaction transaction)
        {
            var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
            var transactionReadOnlyExecutionService =
                _serviceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();
            var blockTimeProvider = _serviceProvider.GetRequiredService<IBlockTimeProvider>();

            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            return await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = preBlock.GetHash(),
                    BlockHeight = preBlock.Height
                },
                transaction,
                blockTimeProvider.GetBlockTime());
        }
    }
}