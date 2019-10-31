﻿using System;
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
            var transactionResourceList = new List<(Transaction, TransactionResourceInfo)>();
            foreach (var t in transactions)
            {
                var transactionResourcePair = await GetResourcesForOneWithCacheAsync(chainContext, t, ct);
                transactionResourceList.Add(transactionResourcePair);
            }

            return transactionResourceList;
        }

        private async Task<(Transaction, TransactionResourceInfo)> GetResourcesForOneWithCacheAsync(
            IChainContext chainContext,
            Transaction transaction, CancellationToken ct)
        {
            if (ct.IsCancellationRequested || _smartContractExecutiveService.IsContractDeployOrUpdating(transaction.To))
                return (transaction, new TransactionResourceInfo()
                {
                    TransactionId = transaction.GetHash(),
                    ParallelType = ParallelType.NonParallelizable
                });

            if (_resourceCache.TryGetValue(transaction.GetHash(), out var resourceCache))
            {
                resourceCache.ResourceUsedBlockHeight = chainContext.BlockHeight;
                return (transaction, resourceCache.ResourceInfo);
            }

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
                        ParallelType = ParallelType.NonParallelizable
                    };
                }

                var resourceInfo = await executive.GetTransactionResourceInfoAsync(chainContext, transaction);
                // Try storing in cache here
                return resourceInfo;
            }
            catch (SmartContractFindRegistrationException)
            {
                return new TransactionResourceInfo
                {
                    TransactionId = transaction.GetHash(),
                    ParallelType = ParallelType.InvalidContractAddress
                };
            }
            finally
            {
                if (executive != null)
                {
                    await _smartContractExecutiveService.PutExecutiveAsync(address, executive);
                }
            }
        }

        public void ClearConflictingTransactionsResourceCache(IEnumerable<Hash> transactionIds,IEnumerable<Address> contractAddresses)
        {
            ClearResourceCache(transactionIds);
            var keys = _resourceCache.Where(c =>
                contractAddresses.Contains(c.Value.Address) &&
                c.Value.ResourceInfo.ParallelType != ParallelType.NonParallelizable).Select(c => c.Key);
            foreach (var key in keys)
            {
                _resourceCache[key].ResourceInfo.ParallelType = ParallelType.NonParallelizable;
            }
        }

        #region Event Handler Methods

        public async Task HandleTransactionAcceptedEvent(TransactionAcceptedEvent eventData)
        {
            var chainContext = await GetChainContextAsync();
            var transaction = eventData.Transaction;

            var resourceInfo = await GetResourcesForOneAsync(chainContext, transaction, CancellationToken.None);
            _resourceCache.TryAdd(transaction.GetHash(),
                new TransactionResourceCache(resourceInfo, transaction.To,
                    eventData.Transaction.GetExpiryBlockNumber()));
        }

        public async Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            var contractInfoCache = _smartContractExecutiveService.GetContractInfoCache();
            var addresses = contractInfoCache.Where(c => c.Value <= eventData.BlockHeight).Select(c => c.Key).ToArray();

            try
            {
                var transactionIds = _resourceCache.Where(c =>
                    c.Value.Address.IsIn(addresses) &&
                    c.Value.ResourceInfo.ParallelType != ParallelType.NonParallelizable).Select(c => c.Key);

                ClearResourceCache(transactionIds.Concat(_resourceCache
                    .Where(c => c.Value.ResourceUsedBlockHeight <= eventData.BlockHeight)
                    .Select(c => c.Key)).Distinct().ToList());

                _smartContractExecutiveService.ClearContractInfoCache(eventData.BlockHeight);
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, "Unexpected case occured when clear resource info.");
            }

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
        public long ResourceUsedBlockHeight { get; set; }
        public readonly TransactionResourceInfo ResourceInfo;
        public readonly Address Address;

        public TransactionResourceCache(TransactionResourceInfo resourceInfo, Address address,
            long resourceUsedBlockHeight)
        {
            ResourceUsedBlockHeight = resourceUsedBlockHeight;
            ResourceInfo = resourceInfo;
            Address = address;
        }
    }
}