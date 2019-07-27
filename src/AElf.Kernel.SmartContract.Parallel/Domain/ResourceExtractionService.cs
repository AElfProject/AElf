using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel
{
    public class ResourceExtractionService : IResourceExtractionService, ISingletonDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly ICodeRemarksManager _codeRemarksManager;
        public ILogger<ResourceExtractionService> Logger { get; set; }

        // TODO: use non concurrent version
        private readonly ConcurrentDictionary<Hash, TransactionResourceCache> _resourceCache =
            new ConcurrentDictionary<Hash, TransactionResourceCache>();

        public ResourceExtractionService(IBlockchainService blockchainService,
            ISmartContractExecutiveService smartContractExecutiveService, ICodeRemarksManager codeRemarksManager)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
            _codeRemarksManager = codeRemarksManager;
            _blockchainService = blockchainService;

            Logger = NullLogger<ResourceExtractionService>.Instance;
        }

        public async Task<IEnumerable<(Transaction, TransactionResourceInfo)>> GetResourcesAsync(
            IChainContext chainContext,
            IEnumerable<Transaction> transactions, CancellationToken ct)
        {
            // Parallel processing below (adding AsParallel) causes ReflectionTypeLoadException
            var tasks = transactions.Select(t => GetResourcesForOneWithCacheAsync(chainContext, t, ct));
            return await Task.WhenAll(tasks);
        }

        private async Task<(Transaction, TransactionResourceInfo)> GetResourcesForOneWithCacheAsync(
            IChainContext chainContext,
            Transaction transaction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return (transaction, new TransactionResourceInfo()
                {
                    TransactionId = transaction.GetHash(),
                    NonParallelizable = true
                });

            if (_resourceCache.TryGetValue(transaction.GetHash(), out var resourceCache))
                return (transaction, resourceCache.ResourceInfo);

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
                var codeRemarks = await _codeRemarksManager.GetCodeRemarksAsync(executive.ContractHash);
                if (codeRemarks != null && codeRemarks.NonParallelizable)
                {
                    return new TransactionResourceInfo
                    {
                        TransactionId = transaction.GetHash(),
                        NonParallelizable = true
                    };
                }

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

        public void ClearConflictingTransactionsResourceCache(IEnumerable<Hash> transactionIds)
        {
            ClearResourceCache(transactionIds);
        }

        #region Event Handler Methods

        public async Task HandleTransactionAcceptedEvent(TransactionAcceptedEvent eventData)
        {
            var chainContext = await GetChainContextAsync();
            var transaction = eventData.Transaction;

            _resourceCache.TryAdd(transaction.GetHash(),
                new TransactionResourceCache(transaction.RefBlockNumber,
                    await GetResourcesForOneAsync(chainContext, transaction, CancellationToken.None)));
        }

        public async Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            ClearResourceCache(_resourceCache.Where(c => c.Value.RefBlockNumber <= eventData.BlockHeight)
                .Select(c => c.Key));

            await Task.CompletedTask;
        }

        public async Task HandleUnexecutableTransactionsFoundAsync(UnexecutableTransactionsFoundEvent eventData)
        {
            ClearResourceCache(eventData.Transactions);

            await Task.CompletedTask;
        }

        public async Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData)
        {
            var block = await _blockchainService.GetBlockByHashAsync(eventData.BlockHeader.GetHash());

            ClearResourceCache(block.TransactionIds);
            
            await Task.CompletedTask;
        }

        private void ClearResourceCache(IEnumerable<Hash> transactions)
        {
            foreach (var transactionId in transactions)
            {
                _resourceCache.TryRemove(transactionId, out _);
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

    internal class TransactionResourceCache
    {
        public readonly long RefBlockNumber;
        public readonly TransactionResourceInfo ResourceInfo;

        public TransactionResourceCache(long refBlockNumber, TransactionResourceInfo resourceInfo)
        {
            RefBlockNumber = refBlockNumber;
            ResourceInfo = resourceInfo;
        }
    }
}