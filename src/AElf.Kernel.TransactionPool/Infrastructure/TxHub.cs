using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TxHub : ITxHub, ISingletonDependency
    {
        public ILogger<TxHub> Logger { get; set; }
        private readonly TransactionOptions _transactionOptions;

        private readonly ITransactionManager _transactionManager;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionValidationService _transactionValidationService;

        private readonly ConcurrentDictionary<Hash, QueuedTransaction> _allTransactions =
            new ConcurrentDictionary<Hash, QueuedTransaction>();

        private ConcurrentDictionary<Hash, QueuedTransaction> _validatedTransactions =
            new ConcurrentDictionary<Hash, QueuedTransaction>();

        private ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>> _invalidatedByBlock =
            new ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>();

        private ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>> _expiredByExpiryBlock =
            new ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>();

        private readonly TransformBlock<QueuedTransaction, QueuedTransaction> _processTransactionJobs;

        private long _bestChainHeight = AElfConstants.GenesisBlockHeight - 1;
        private Hash _bestChainHash = Hash.Empty;

        private List<TransformBlock<QueuedTransaction, QueuedTransaction>> _validationTransformBlockList =
            new List<TransformBlock<QueuedTransaction, QueuedTransaction>>();

        public ILocalEventBus LocalEventBus { get; set; }

        public TxHub(ITransactionManager transactionManager, IBlockchainService blockchainService,
            IOptionsSnapshot<TransactionOptions> transactionOptions,
            ITransactionValidationService transactionValidationService)
        {
            Logger = NullLogger<TxHub>.Instance;
            _transactionManager = transactionManager;
            _blockchainService = blockchainService;
            _transactionValidationService = transactionValidationService;
            LocalEventBus = NullLocalEventBus.Instance;
            _transactionOptions = transactionOptions.Value;
            _processTransactionJobs = CreateQueuedTransactionBufferBlock();
        }

        public async Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(int transactionCount = 0)
        {
            var output = new ExecutableTransactionSet
            {
                PreviousBlockHash = _bestChainHash,
                PreviousBlockHeight = _bestChainHeight
            };

            if (transactionCount < 0)
            {
                return output;
            }

            // TODO: It does not make sense to compare _bestChainHash with chain, especially when it comes to parallel block execution. Branch hash through interface params should be used instead. 
            var chain = await _blockchainService.GetChainAsync();
            if (chain.BestChainHash != _bestChainHash)
            {
                Logger.LogWarning(
                    $"Attempting to retrieve executable transactions while best chain records don't match.");
                return output;
            }

            output.Transactions.AddRange(_validatedTransactions.Values.OrderBy(x => x.EnqueueTime)
                .Take(transactionCount == 0 ? int.MaxValue : transactionCount)
                .Select(x => x.Transaction));

            return output;
        }
        
        public async Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            if (_bestChainHash == Hash.Empty)
                return;

            foreach (var transaction in transactions)
            {
                var queuedTransaction = new QueuedTransaction
                {
                    TransactionId = transaction.GetHash(),
                    Transaction = transaction,
                    EnqueueTime = TimestampHelper.GetUtcNow()
                };
                var sendResult = await _processTransactionJobs.SendAsync(queuedTransaction);
                if (!sendResult)
                {
                    Logger.LogWarning($"Process transaction:{queuedTransaction.TransactionId} failed.");
                }
            }
        }

        public async Task UpdateTransactionPoolByBestChainAsync(Hash bestChainHash, long bestChainHeight)
        {
            var minimumHeight = _allTransactions.Count == 0
                ? 0
                : _allTransactions.Min(kv => kv.Value.Transaction.RefBlockNumber);
            var prefixes = await GetPrefixesByHeightAsync(minimumHeight, bestChainHash, bestChainHeight);
            ResetCurrentCollections();
            foreach (var queuedTransaction in _allTransactions.Values)
            {
                prefixes.TryGetValue(queuedTransaction.Transaction.RefBlockNumber, out var prefix);
                queuedTransaction.RefBlockStatus =
                    CheckRefBlockStatus(queuedTransaction.Transaction, prefix, bestChainHeight);
                AddToCollection(queuedTransaction);
            }

            CleanTransactions(_expiredByExpiryBlock, bestChainHeight);

            _bestChainHash = bestChainHash;
            _bestChainHeight = bestChainHeight;
        }

        public Task CleanByHeightAsync(long height)
        {
            CleanTransactions(_expiredByExpiryBlock, height);
            CleanTransactions(_invalidatedByBlock, height);

            return Task.CompletedTask;
        }

        public Task CleanByTransactionIdsAsync(IEnumerable<Hash> transactionIds)
        {
            CleanTransactions(transactionIds);
            return Task.CompletedTask;
        }

        public Task<TransactionPoolStatus> GetTransactionPoolStatusAsync()
        {
            return Task.FromResult(new TransactionPoolStatus
            {
                AllTransactionCount = _allTransactions.Count,
                ValidatedTransactionCount = _validatedTransactions.Count
            });
        }

        public Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId)
        {
            _allTransactions.TryGetValue(transactionId, out var receipt);
            return Task.FromResult(receipt);
        }

        #region Private Methods

        private static void AddToCollection(
            ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>> collection,
            QueuedTransaction receipt)
        {
            if (!collection.TryGetValue(receipt.Transaction.RefBlockNumber, out var receipts))
            {
                receipts = new ConcurrentDictionary<Hash, QueuedTransaction>();
                collection.TryAdd(receipt.Transaction.RefBlockNumber, receipts);
            }

            receipts.TryAdd(receipt.TransactionId, receipt);
        }

        private static RefBlockStatus CheckRefBlockStatus(Transaction transaction, ByteString prefix,
            long bestChainHeight)
        {
            if (transaction.GetExpiryBlockNumber() <= bestChainHeight)
            {
                return RefBlockStatus.RefBlockExpired;
            }

            return transaction.RefBlockPrefix == prefix ? RefBlockStatus.RefBlockValid : RefBlockStatus.RefBlockInvalid;
        }

        private ByteString GetPrefixByHash(Hash hash)
        {
            return hash == null ? null : BlockHelper.GetRefBlockPrefix(hash);
        }

        private async Task<ByteString> GetPrefixByHeightAsync(long height, Hash bestChainHash)
        {
            var chain = await _blockchainService.GetChainAsync();
            var hash = await _blockchainService.GetBlockHashByHeightAsync(chain, height, bestChainHash);
            return GetPrefixByHash(hash);
        }

        private async Task<Dictionary<long, ByteString>> GetPrefixesByHeightAsync(long firstHeight, Hash bestChainHash,
            long bestChainHeight)
        {
            var blockIndexes =
                await _blockchainService.GetBlockIndexesAsync(firstHeight, bestChainHash, bestChainHeight);

            return blockIndexes.ToDictionary(blockIndex => blockIndex.BlockHeight,
                blockIndex => GetPrefixByHash(blockIndex.BlockHash));
        }

        private void ResetCurrentCollections()
        {
            _expiredByExpiryBlock = new ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>();
            _invalidatedByBlock = new ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>();
            _validatedTransactions = new ConcurrentDictionary<Hash, QueuedTransaction>();
        }

        private void AddToCollection(QueuedTransaction queuedTransaction)
        {
            switch (queuedTransaction.RefBlockStatus)
            {
                case RefBlockStatus.RefBlockExpired:
                    AddToCollection(_expiredByExpiryBlock, queuedTransaction);
                    break;
                case RefBlockStatus.RefBlockInvalid:
                    AddToCollection(_invalidatedByBlock, queuedTransaction);
                    break;
                case RefBlockStatus.RefBlockValid:
                    _validatedTransactions.TryAdd(queuedTransaction.TransactionId, queuedTransaction);
                    break;
            }
        }

        private void CleanTransactions(ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>
            collection, long blockHeight)
        {
            foreach (var txIds in collection.Where(kv => kv.Key <= blockHeight))
            {
                CleanTransactions(txIds.Value.Keys.ToList());
            }
        }

        private void CleanTransactions(IEnumerable<Hash> transactionIds)
        {
            foreach (var transactionId in transactionIds)
            {
                _allTransactions.TryRemove(transactionId, out _);
            }
        }

        #endregion

        #region Data flow

        private TransformBlock<QueuedTransaction, QueuedTransaction> CreateQueuedTransactionBufferBlock()
        {
            var executionDataFlowBlockOptions = new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = _transactionOptions.PoolLimit,
                MaxDegreeOfParallelism = _transactionOptions.PoolParallelismDegree
            };
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            var updateBucketIndexTransformBlock =
                new TransformBlock<QueuedTransaction, QueuedTransaction>(UpdateBucketIndex,
                    executionDataFlowBlockOptions);
            var updateRefBlockStatusActionBlock = new ActionBlock<QueuedTransaction>(
                async queuedTransaction =>
                    await ProcessQueuedTransactionAsync(queuedTransaction, UpdateQueuedTransactionRefBlockStatusAsync),
                executionDataFlowBlockOptions);
            int i = 0;
            while (i < _transactionOptions.PoolParallelismDegree)
            {
                var validationTransformBlock = new TransformBlock<QueuedTransaction, QueuedTransaction>(
                    async queuedTransaction =>
                        await ProcessQueuedTransactionAsync(queuedTransaction, AcceptTransactionAsync),
                    new ExecutionDataflowBlockOptions
                    {
                        BoundedCapacity = _transactionOptions.PoolLimit,
                        EnsureOrdered = false
                    });
                _validationTransformBlockList.Add(validationTransformBlock);
                var index = i;
                updateBucketIndexTransformBlock.LinkTo(validationTransformBlock, linkOptions,
                    queuedTransaction =>
                    {
                        var bucketHit = queuedTransaction.BucketIndex == index ||
                                        queuedTransaction.BucketIndex == -index;
                        if (bucketHit)
                            Logger.LogDebug(
                                $"Transaction {queuedTransaction.TransactionId}, enqueue time {queuedTransaction.EnqueueTime} hits the bucket {index}." +
                                $"validation transform block input count {validationTransformBlock.InputCount}, output count {validationTransformBlock.OutputCount}");
                                
                        return bucketHit;
                    });

                validationTransformBlock.LinkTo(updateRefBlockStatusActionBlock, linkOptions,
                    queuedTransaction => queuedTransaction != null);
                validationTransformBlock.LinkTo(DataflowBlock.NullTarget<QueuedTransaction>());
                i++;
            }

            updateBucketIndexTransformBlock.LinkTo(DataflowBlock.NullTarget<QueuedTransaction>());
            return updateBucketIndexTransformBlock;
        }

        private QueuedTransaction UpdateBucketIndex(QueuedTransaction queuedTransaction)
        {
            Logger.LogDebug($"UpdateBucketIndex-->queuedTransaction id is {queuedTransaction.TransactionId}");

            queuedTransaction.BucketIndex =
                Math.Abs(queuedTransaction.TransactionId.ToInt64() % _transactionOptions.PoolParallelismDegree);
            var validationTransformBlock = _validationTransformBlockList[(int) queuedTransaction.BucketIndex];
            Logger.LogDebug(
                $"UpdateBucketIndex-->queuedTransaction id is {queuedTransaction.TransactionId}, BucketIndex is {queuedTransaction.BucketIndex}, " +
                $"validation transform block input count {validationTransformBlock.InputCount}, output count {validationTransformBlock.OutputCount}");
            return queuedTransaction;
        }

        private async Task<bool> VerifyTransactionAcceptableAsync(QueuedTransaction queuedTransaction)
        {
            return _allTransactions.Count < _transactionOptions.PoolLimit &&
                   !_allTransactions.ContainsKey(queuedTransaction.TransactionId) &&
                   queuedTransaction.Transaction.VerifyExpiration(_bestChainHeight) &&
                   !await _blockchainService.HasTransactionAsync(queuedTransaction.TransactionId);
        }

        private async Task<QueuedTransaction> AcceptTransactionAsync(QueuedTransaction queuedTransaction)
        {
            try
            {
                var index = (int) (queuedTransaction.BucketIndex >= 0
                    ? queuedTransaction.BucketIndex
                    : -queuedTransaction.BucketIndex);
                var validationTransformBlock = _validationTransformBlockList[index];
                Logger.LogDebug(
                    $"AcceptTransactionAsync-->queuedTransaction id is {queuedTransaction.TransactionId}; queuedTransaction BucketIndex is {queuedTransaction.BucketIndex}, " +
                    $"validation transform block input count {validationTransformBlock.InputCount}, output count {validationTransformBlock.OutputCount}");
                
                if (!await VerifyTransactionAcceptableAsync(queuedTransaction))
                {
                    Logger.LogDebug($"AcceptTransactionAsync-->VerifyTransactionAcceptableAsync: "+
                    $"Transaction id is { queuedTransaction.TransactionId }; "+
                    $"queuedTransaction BucketIndex is { queuedTransaction.BucketIndex }; "+        
                    $"EnqueueTime is { queuedTransaction.EnqueueTime }; "+    
                    $"RefBlockStatus is { queuedTransaction.RefBlockStatus }; "+   
                    $"Transaction From is { queuedTransaction.Transaction.From }; "+   
                    $"Transaction To is { queuedTransaction.Transaction.To }");
                    return null;
                }

                var validationResult =
                    await _transactionValidationService.ValidateTransactionWhileCollectingAsync(new ChainContext
                    {
                        BlockHash = _bestChainHash,
                        BlockHeight = _bestChainHeight
                    }, queuedTransaction.Transaction);
                if (!validationResult)
                {
                    Logger.LogWarning($"Transaction {queuedTransaction.TransactionId} validation failed.");
                    return null;
                }

                // double check
                var hasTransaction = await _blockchainService.HasTransactionAsync(queuedTransaction.TransactionId);
                if (hasTransaction)
                {
                    Logger.LogDebug($"AcceptTransactionAsync-->hasTransaction is {hasTransaction}: " +
                    $"Transaction id is { queuedTransaction.TransactionId }; "+
                    $"queuedTransaction BucketIndex is { queuedTransaction.BucketIndex }; "+        
                    $"EnqueueTime is { queuedTransaction.EnqueueTime }; "+    
                    $"RefBlockStatus is { queuedTransaction.RefBlockStatus }; "+   
                    $"Transaction From is { queuedTransaction.Transaction.From }; "+   
                    $"Transaction To is { queuedTransaction.Transaction.To }");
                    return null;
                }

                await _transactionManager.AddTransactionAsync(queuedTransaction.Transaction);
                var addSuccess = _allTransactions.TryAdd(queuedTransaction.TransactionId, queuedTransaction);
                if (addSuccess)
                {
                    Logger.LogDebug($"AcceptTransactionAsync-->addSuccess is True"  +
                    $"Transaction id is { queuedTransaction.TransactionId }; "+
                    $"queuedTransaction BucketIndex is { queuedTransaction.BucketIndex }; "+        
                    $"EnqueueTime is { queuedTransaction.EnqueueTime }; "+    
                    $"RefBlockStatus is { queuedTransaction.RefBlockStatus }; "+   
                    $"Transaction From is { queuedTransaction.Transaction.From }; "+   
                    $"Transaction To is { queuedTransaction.Transaction.To }");
                    return queuedTransaction;
                }

                Logger.LogWarning($"Transaction {queuedTransaction.TransactionId} insert failed.");
                Logger.LogDebug($"AcceptTransactionAsync-->addSuccess is False" +
                    $"Transaction id is { queuedTransaction.TransactionId }; "+
                    $"queuedTransaction BucketIndex is { queuedTransaction.BucketIndex }; "+        
                    $"EnqueueTime is { queuedTransaction.EnqueueTime }; "+    
                    $"RefBlockStatus is { queuedTransaction.RefBlockStatus }; "+   
                    $"Transaction From is { queuedTransaction.Transaction.From }; "+   
                    $"Transaction To is { queuedTransaction.Transaction.To }");
                 return null;
            }
            catch (Exception e)
            {
                Logger.LogDebug($"AcceptTransactionAsync-->Catch: "+
                    $"Transaction id is { queuedTransaction.TransactionId }; "+
                    $"queuedTransaction BucketIndex is { queuedTransaction.BucketIndex }; "+        
                    $"EnqueueTime is { queuedTransaction.EnqueueTime }; "+    
                    $"RefBlockStatus is { queuedTransaction.RefBlockStatus }; "+   
                    $"Transaction From is { queuedTransaction.Transaction.From }; "+   
                    $"Transaction To is { queuedTransaction.Transaction.To }");

                Logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<QueuedTransaction> UpdateQueuedTransactionRefBlockStatusAsync(
            QueuedTransaction queuedTransaction)
        {
            Logger.LogDebug($"UpdateQueuedTransactionRefBlockStatusAsync-->queuedTransaction id is {queuedTransaction.TransactionId}");
            var prefix = await GetPrefixByHeightAsync(queuedTransaction.Transaction.RefBlockNumber, _bestChainHash);
            queuedTransaction.RefBlockStatus =
                CheckRefBlockStatus(queuedTransaction.Transaction, prefix, _bestChainHeight);

            if (queuedTransaction.RefBlockStatus == RefBlockStatus.RefBlockValid)
            {
                await LocalEventBus.PublishAsync(new TransactionAcceptedEvent
                {
                    Transaction = queuedTransaction.Transaction
                });
            }
            Logger.LogDebug($"UpdateQueuedTransactionRefBlockStatusAsync-->queuedTransaction id is {queuedTransaction.TransactionId}; queuedTransaction BucketIndex is { queuedTransaction.BucketIndex }");

            return queuedTransaction;
        }

        private async Task<QueuedTransaction> ProcessQueuedTransactionAsync(QueuedTransaction queuedTransaction,
            Func<QueuedTransaction, Task<QueuedTransaction>> func)
        {
            try
            {
                return await func(queuedTransaction);
            }
            catch (Exception e)
            {
                Logger.LogError(e,
                    $"Unacceptable transaction {queuedTransaction.TransactionId}. Func: {func?.Method.Name}");
                return null;
            }
        }

        #endregion
    }
}