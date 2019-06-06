using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ResourceExtractionService : IResourceExtractionService, ISingletonDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        public ILogger<TransactionGrouper> Logger { get; set; }

        private readonly ConcurrentDictionary<Hash, TransactionResourceInfo> _resourceCache = 
            new ConcurrentDictionary<Hash, TransactionResourceInfo>();

        public ResourceExtractionService(IBlockchainService blockchainService, 
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
            _blockchainService = blockchainService;
        }

        public async Task<IEnumerable<(Transaction, TransactionResourceInfo)>> GetResourcesAsync(IChainContext chainContext,
            IEnumerable<Transaction> transactions, CancellationToken ct)
        {
            // Parallel processing below (adding AsParallel) causes ReflectionTypeLoadException
            var tasks = transactions.Select(t => GetResourcesForOneWithCacheAsync(chainContext, t, ct));
            return await Task.WhenAll(tasks);
        }

        private async Task<(Transaction, TransactionResourceInfo)> GetResourcesForOneWithCacheAsync(IChainContext chainContext,
            Transaction transaction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return (transaction, new TransactionResourceInfo()
                {
                    TransactionId = transaction.GetHash(),
                    NonParallelizable = true
                });

            if (_resourceCache.TryGetValue(transaction.GetHash(), out var resourceInfo))
                return (transaction, resourceInfo);

            return (transaction, await GetResourcesForOneAsync(chainContext, transaction, ct));
        }
        
        private async Task<TransactionResourceInfo> GetResourcesForOneAsync(IChainContext chainContext,
            Transaction transaction, CancellationToken ct)
        {
            IExecutive executive = null;
            var address = transaction.To;
            
            try
            {
                executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, address);
                var resourceInfo = await executive.GetTransactionResourceInfoAsync(chainContext, transaction);
                // Try storing in cache here
                return resourceInfo;
            }
            finally
            {
                if (executive != null)
                {
                    await _smartContractExecutiveService.PutExecutiveAsync(address, executive);
                }
            }
        }

        #region Event Handler Methods
        
        public async Task HandleTransactionResourcesNeededAsync(TransactionResourcesNeededEvent eventData)
        {
            var chainContext = await GetChainContextAsync();
            
            foreach (var tx in eventData.Transactions)
            {
                _resourceCache.TryAdd(tx.GetHash(), await GetResourcesForOneAsync(chainContext, tx, CancellationToken.None));
            }

            Logger.LogTrace($"Resource cache size current: {_resourceCache.Count}");
        }

        public async Task HandleTransactionResourcesNoLongerNeededAsync(TransactionResourcesNoLongerNeededEvent eventData)
        {
            foreach (var txId in eventData.TxIds)
            {
                _resourceCache.TryRemove(txId, out _);
            }
            
            Logger.LogTrace($"Resource cache size after cleanup: {_resourceCache.Count}");
        }
        #endregion
        
        private async Task<ChainContext> GetChainContextAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            if (chain == null)
            {
                return null;
            }

            var chainContext = new ChainContext()
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            return chainContext;
        }
    }
}
