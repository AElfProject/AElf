using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.TestKit
{
    public class TransactionExecutor : ITransactionExecutor
    {
        private readonly IServiceProvider _serviceProvider;

        public TransactionExecutor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task ExecuteAsync(Transaction transaction)
        {
            var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var miningService = _serviceProvider.GetRequiredService<IMiningService>();
            var blockAttachService = _serviceProvider.GetRequiredService<IBlockAttachService>();

            var block = await miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = preBlock.GetHash(), PreviousBlockHeight = preBlock.Height,
                    BlockExecutionTime = TimeSpan.FromMilliseconds(int.MaxValue)
                },
                new List<Transaction> {transaction},
                DateTime.UtcNow);

            await blockchainService.AddTransactionsAsync(new List<Transaction> {transaction});
            await blockchainService.AddBlockAsync(block);
            await blockAttachService.AttachBlockAsync(block);
        }

        public async Task<ByteString> ReadAsync(Transaction transaction)
        {
            var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
            var transactionReadOnlyExecutionService =
                _serviceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();
            var blockTimeProvider = _serviceProvider.GetRequiredService<IBlockTimeProvider>();

            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var transactionTrace = await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = preBlock.GetHash(),
                    BlockHeight = preBlock.Height
                },
                transaction,
                blockTimeProvider.GetBlockTime());

            return transactionTrace.ReturnValue;
        }
    }
}