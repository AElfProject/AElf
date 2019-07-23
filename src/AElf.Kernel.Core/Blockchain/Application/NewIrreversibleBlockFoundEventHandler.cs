using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Blockchain.Application
{
    public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>, ITransientDependency
    {
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly ITransactionBlockIndexManager _transactionBlockIndexManager;
        private readonly IBlockchainService _blockchainService;

        public NewIrreversibleBlockFoundEventHandler(ITransactionResultManager transactionResultManager,
            ITransactionBlockIndexManager transactionBlockIndexManager,
            IBlockchainService blockchainService)
        {
            _transactionResultManager = transactionResultManager;
            _transactionBlockIndexManager = transactionBlockIndexManager;
            _blockchainService = blockchainService;
        }
        
        public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            var blockHash = eventData.BlockHash;
            while (true)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockHash);

                var preMiningHash = block.Header.GetPreMiningHash();
                var transactionBlockIndex = new TransactionBlockIndex()
                {
                    BlockHash = blockHash
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
                        await _transactionResultManager.AddTransactionResultAsync(result, transactionBlockIndex.BlockHash);
                    }
                }

                if (withPreMiningHash != null)
                {
                    // TransactionResult is saved with PreMiningHash
                    // Remove results saved with PreMiningHash, as it will never be queried
                    foreach (var txId in block.Body.TransactionIds)
                    {
                        await _transactionResultManager.RemoveTransactionResultAsync(txId, preMiningHash);
                    }
                }

                // Add TransactionBlockIndex
                foreach (var txId in block.Body.TransactionIds)
                {
                    await _transactionBlockIndexManager.SetTransactionBlockIndexAsync(txId, transactionBlockIndex);
                }

                if (block.Height <= eventData.PreviousIrreversibleBlockHeight)
                {
                    break;
                }

                blockHash = block.Header.PreviousBlockHash;
            }
        }
    }
}