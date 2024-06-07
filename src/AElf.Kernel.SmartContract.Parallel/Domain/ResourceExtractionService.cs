using System;
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
using AElf.Kernel.TransactionPool;
using AElf.Standards.ACS2;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Parallel;

public class ResourceExtractionService : IResourceExtractionService, ISingletonDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly INonparallelContractCodeProvider _nonparallelContractCodeProvider;

    private readonly ConcurrentDictionary<Hash, TransactionResourceCache> _resourceCache = new();

    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly ITransactionContextFactory _transactionContextFactory;

    public ResourceExtractionService(IBlockchainService blockchainService,
        ISmartContractExecutiveService smartContractExecutiveService,
        INonparallelContractCodeProvider nonparallelContractCodeProvider,
        ITransactionContextFactory transactionContextFactory)
    {
        _smartContractExecutiveService = smartContractExecutiveService;
        _nonparallelContractCodeProvider = nonparallelContractCodeProvider;
        _transactionContextFactory = transactionContextFactory;
        _blockchainService = blockchainService;

        Logger = NullLogger<ResourceExtractionService>.Instance;
    }

    public ILogger<ResourceExtractionService> Logger { get; set; }

    public async Task<IEnumerable<TransactionWithResourceInfo>> GetResourcesAsync(
        IChainContext chainContext,
        IEnumerable<Transaction> transactions, CancellationToken ct)
    {
        // Parallel processing below (adding AsParallel) causes ReflectionTypeLoadException
        var transactionResourceList = new List<TransactionWithResourceInfo>();
        var contractResourceInfoCache = new Dictionary<Address, ContractResourceInfo>();
        foreach (var t in transactions)
        {
            var transactionResourcePair =
                await GetResourcesForOneWithCacheAsync(chainContext, t, ct, contractResourceInfoCache);
            transactionResourceList.Add(transactionResourcePair);
        }

        return transactionResourceList;
    }

    // TODO: Fix
    // public async Task<IEnumerable<TransactionWithResourceInfo>> GetResourcesAsync(
    //     IChainContext chainContext,
    //     IEnumerable<Transaction> transactions, CancellationToken ct)
    // {
    //     var contractResourceInfoCache = new ConcurrentDictionary<Address, ContractResourceInfo>();
    //     var transactionResourceList = await transactions.AsParallel().WithCancellation(ct).Select(async transaction =>
    //     {
    //         var transactionResourcePair =
    //             await GetResourcesForOneWithCacheAsync(chainContext, transaction, ct, contractResourceInfoCache);
    //         return transactionResourcePair;
    //     }).WhenAll();
    //
    //     return transactionResourceList;
    // }

    public void ClearConflictingTransactionsResourceCache(IEnumerable<Hash> transactionIds)
    {
        ClearResourceCache(transactionIds);
    }

    private async Task<TransactionWithResourceInfo> GetResourcesForOneWithCacheAsync(
        IChainContext chainContext,
        Transaction transaction, CancellationToken ct,
        Dictionary<Address, ContractResourceInfo> contractResourceInfoCache)
    {
        if (ct.IsCancellationRequested)
            return new TransactionWithResourceInfo
            {
                Transaction = transaction,
                TransactionResourceInfo = new TransactionResourceInfo
                {
                    TransactionId = transaction.GetHash(),
                    ParallelType = ParallelType.NonParallelizable
                }
            };

        if (_resourceCache.TryGetValue(transaction.GetHash(), out var resourceCache))
            if (contractResourceInfoCache.TryGetValue(transaction.To, out var contractResourceInfo))
                if (resourceCache.ResourceInfo.ContractHash == contractResourceInfo.CodeHash &&
                    resourceCache.ResourceInfo.IsNonparallelContractCode ==
                    contractResourceInfo.IsNonparallelContractCode)
                    return new TransactionWithResourceInfo
                    {
                        Transaction = transaction,
                        TransactionResourceInfo = resourceCache.ResourceInfo
                    };

        var resourceInfo = await GetResourcesForOneAsync(chainContext, transaction, ct);
        if (!contractResourceInfoCache.TryGetValue(transaction.To, out _))
            contractResourceInfoCache[transaction.To] = new ContractResourceInfo
            {
                CodeHash = resourceInfo.ContractHash,
                IsNonparallelContractCode = resourceInfo.IsNonparallelContractCode
            };

        return new TransactionWithResourceInfo
        {
            Transaction = transaction,
            TransactionResourceInfo = resourceInfo
        };
    }

    private async Task<TransactionResourceInfo> GetResourcesForOneAsync(IChainContext chainContext,
        Transaction transaction, CancellationToken ct)
    {
        IExecutive executive = null;
        var address = transaction.To;

        try
        {
            executive = await _smartContractExecutiveService.GetExecutiveAsync(chainContext, address);
            if (!executive.IsParallelizable())
                return new TransactionResourceInfo
                {
                    TransactionId = transaction.GetHash(),
                    ParallelType = ParallelType.NonParallelizable,
                    ContractHash = executive.ContractHash
                };

            var nonparallelContractCode =
                await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(chainContext, address);
            if (nonparallelContractCode != null && nonparallelContractCode.CodeHash == executive.ContractHash)
                return new TransactionResourceInfo
                {
                    TransactionId = transaction.GetHash(),
                    ParallelType = ParallelType.NonParallelizable,
                    ContractHash = executive.ContractHash,
                    IsNonparallelContractCode = true
                };

            if (_resourceCache.TryGetValue(transaction.GetHash(), out var resourceCache) &&
                executive.ContractHash == resourceCache.ResourceInfo.ContractHash &&
                resourceCache.ResourceInfo.IsNonparallelContractCode == false)
                return resourceCache.ResourceInfo;

            var txContext = GetTransactionContext(chainContext, transaction.To, transaction.ToByteString());
            var resourceInfo = await executive.GetTransactionResourceInfoAsync(txContext, transaction.GetHash());
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
                await _smartContractExecutiveService.PutExecutiveAsync(chainContext, address, executive);
        }
    }

    private async Task<ChainContext> GetChainContextAsync()
    {
        var chain = await _blockchainService.GetChainAsync();
        if (chain == null) return null;

        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        return chainContext;
    }

    private ITransactionContext GetTransactionContext(IChainContext chainContext, Address contractAddress,
        ByteString param)
    {
        var generatedTxn = new Transaction
        {
            From = contractAddress,
            To = contractAddress,
            MethodName = nameof(ACS2BaseContainer.ACS2BaseStub.GetResourceInfo),
            Params = param,
            Signature = ByteString.CopyFromUtf8(KernelConstants.SignaturePlaceholder)
        };

        var txContext = _transactionContextFactory.Create(generatedTxn, chainContext);
        return txContext;
    }

    private class ContractResourceInfo
    {
        public Hash CodeHash { get; set; }

        public bool IsNonparallelContractCode { get; set; }
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
        try
        {
            ClearResourceCache(_resourceCache
                //.AsParallel()
                .Where(c => c.Value.ResourceUsedBlockHeight <= eventData.BlockHeight)
                .Select(c => c.Key).Distinct().ToList());
        }
        catch (InvalidOperationException e)
        {
            Logger.LogError(e, "Unexpected case occured when clear resource info.");
        }

        await Task.CompletedTask;
    }

    public async Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData)
    {
        ClearResourceCache(eventData.Block.TransactionIds);

        await Task.CompletedTask;
    }

    private void ClearResourceCache(IEnumerable<Hash> transactions)
    {
        foreach (var transactionId in transactions) _resourceCache.TryRemove(transactionId, out _);

        Logger.LogDebug($"Resource cache size after cleanup: {_resourceCache.Count}");
    }

    #endregion
}

internal class TransactionResourceCache
{
    public readonly Address Address;
    public readonly TransactionResourceInfo ResourceInfo;

    public TransactionResourceCache(TransactionResourceInfo resourceInfo, Address address,
        long resourceUsedBlockHeight)
    {
        ResourceUsedBlockHeight = resourceUsedBlockHeight;
        ResourceInfo = resourceInfo;
        Address = address;
    }

    public long ResourceUsedBlockHeight { get; set; }
}