using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Blockchain.Application
{
    public class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly ITransactionBlockIndexService _transactionBlockIndexService;
        private readonly IBlockchainService _blockchainService;
        public ILogger<BestChainFoundEventHandler> Logger { get; set; }

        public BestChainFoundEventHandler(ITransactionResultManager transactionResultManager,
            IBlockchainService blockchainService, ITransactionBlockIndexService transactionBlockIndexService)
        {
            _transactionResultManager = transactionResultManager;
            _blockchainService = blockchainService;
            _transactionBlockIndexService = transactionBlockIndexService;
        }
        
        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            foreach (var blockHash in eventData.ExecutedBlocks)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockHash);
                Logger.LogTrace($"Handle lib for transactions of block {block.Height}");
                
                var preMiningHash = block.Header.GetPreMiningHash();
                var blockIndex = new BlockIndex
                {
                    BlockHash = blockHash,
                    BlockHeight = block.Height
                };
                
                if (block.Body.TransactionIds.Count == 0)
                {
                    // This will only happen during test environment
                    return;
                }

                var firstTransaction = block.Body.TransactionIds.First();
                var withBlockHash = await _transactionResultManager.GetTransactionResultAsync(
                    firstTransaction, blockHash);
                var withPreMiningHash = await _transactionResultManager.GetTransactionResultAsync(
                    firstTransaction, preMiningHash);

                if (withBlockHash == null)
                {
                    // TransactionResult is not saved with real BlockHash
                    // Save results with real (post mining) Hash, so that it can be queried with TransactionBlockIndex
                    foreach (var txId in block.Body.TransactionIds)
                    {
                        var result = await _transactionResultManager.GetTransactionResultAsync(txId, preMiningHash);
                        await _transactionResultManager.AddTransactionResultAsync(result, blockIndex.BlockHash);
                    }
                }
                
                // Add TransactionBlockIndex
                foreach (var txId in block.Body.TransactionIds)
                {
                    if (withPreMiningHash != null)
                    {
                        await _transactionResultManager.RemoveTransactionResultAsync(txId, preMiningHash);
                    }
                    
                    await _transactionBlockIndexService.UpdateTransactionBlockIndexAsync(txId, blockIndex);
                }
            }
        }
    }
}