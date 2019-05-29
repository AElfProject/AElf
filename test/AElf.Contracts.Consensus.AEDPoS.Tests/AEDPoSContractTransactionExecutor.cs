using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.Consensus.AEDPoS
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AEDPoSContractTransactionExecutor : ITransactionExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBlockchainService _blockchainService;

        public AEDPoSContractTransactionExecutor(IServiceProvider serviceProvider, IBlockchainService blockchainService)
        {
            _serviceProvider = serviceProvider;
            _blockchainService = blockchainService;
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
            var blockAttachService = _serviceProvider.GetRequiredService<IBlockAttachService>();
            var blockTimeProvider = _serviceProvider.GetRequiredService<IBlockTimeProvider>();

            var block = await minerService.MineAsync(preBlock.GetHash(), preBlock.Height,
                blockTimeProvider.GetBlockTime().ToTimestamp(), TimeSpan.FromMilliseconds(int.MaxValue));

            await _blockchainService.AddBlockAsync(block);
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
                blockTimeProvider.GetBlockTime().ToTimestamp());

            return transactionTrace.ReturnValue;
        }
    }
}