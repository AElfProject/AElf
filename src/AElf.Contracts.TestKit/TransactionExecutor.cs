using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
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
            var minerService = _serviceProvider.GetRequiredService<IMiningService>();
            var blockAttachService = _serviceProvider.GetRequiredService<IBlockAttachService>();

            var block = await minerService.MineAsync(preBlock.GetHash(), preBlock.Height,
                new List<Transaction> {transaction},
                DateTime.UtcNow, TimeSpan.FromMilliseconds(int.MaxValue));

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