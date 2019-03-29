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
            var txHub = _serviceProvider.GetRequiredService<ITxHub>();
            await txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
            {
                Transactions = new List<Transaction> {transaction}
            });
            var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var minerService = _serviceProvider.GetRequiredService<IMinerService>();
            var blockchainExecutingService = _serviceProvider.GetRequiredService<IBlockchainExecutingService>();

            var block = await minerService.MineAsync(preBlock.GetHash(), preBlock.Height,
                DateTime.UtcNow.AddMilliseconds(int.MaxValue));
            
            await blockchainService.AddBlockAsync(block);
            var chain = await blockchainService.GetChainAsync();
            var status = await blockchainService.AttachBlockToChainAsync(chain, block);
            await blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
        }

        public async Task<ByteString> ReadAsync(Transaction transaction)
        {
            var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
            var transactionReadOnlyExecutionService =
                _serviceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();

            var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
            var transactionTrace = await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = preBlock.GetHash(),
                    BlockHeight = preBlock.Height
                },
                transaction,
                DateTime.UtcNow);

            return transactionTrace.ReturnValue;
        }
    }
}