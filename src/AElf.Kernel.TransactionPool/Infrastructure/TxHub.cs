using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
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

        private readonly List<ConcurrentDictionary<Hash, QueuedTransaction>> _dictList =
            new List<ConcurrentDictionary<Hash, QueuedTransaction>>();

        // private readonly ConcurrentDictionary<Hash, QueuedTransaction> _allTransactions=
        //     new ConcurrentDictionary<Hash, QueuedTransaction>();

        // private ConcurrentDictionary<Hash, QueuedTransaction> _validatedTransactions =
        //     new ConcurrentDictionary<Hash, QueuedTransaction>();

        // private ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>> _invalidatedByBlock =
        //     new ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>();

        private ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>> _expiredByExpiryBlock =
            new ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>();

        // private readonly TransformBlock<QueuedTransaction, QueuedTransaction> _processTransactionJobs;

        private long _bestChainHeight = AElfConstants.GenesisBlockHeight - 1;
        private Hash _bestChainHash = Hash.Empty;

        public ILocalEventBus LocalEventBus { get; set; }
        private ActionBlock<QueuedTransaction> _actionBlock;

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
            _actionBlock = CreateQueuedTransactionBufferBlock();

            for (int i = 0; i < transactionOptions.Value.PoolParallelismDegree; i++)
            {
                _dictList.Add(new ConcurrentDictionary<Hash, QueuedTransaction>());
            }
        }

        public async Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(Hash blockHash,
            int transactionCount)
        {
            Logger.LogTrace("Begin TxHub.GetExecutableTransactionSetAsync");
            var output = new ExecutableTransactionSet
            {
                PreviousBlockHash = _bestChainHash,
                PreviousBlockHeight = _bestChainHeight
            };

            if (transactionCount <= 0)
            {
                return output;
            }

            if (blockHash != _bestChainHash)
            {
                Logger.LogWarning(
                    $"Attempting to retrieve executable transactions while best chain records don't match.");
                return output;
            }

            // Logger.LogDebug($"_validatedTransactions count: {_validatedTransactions.Count}");
            Logger.LogDebug($"_allTransactions count: {GetTxCount()}");

            List<Transaction> list = GetTransactions(transactionCount);
            output.Transactions.AddRange(list);
            Logger.LogTrace("End TxHub.GetExecutableTransactionSetAsync");
            return output;
        }

        private List<Transaction> GetTransactions(int transactionCount)
        {
            var res = new List<Transaction>();

            foreach (var dict in _dictList)
            {
                if (transactionCount <= 0)
                    return res;
                
                var take = dict.Count < transactionCount ? dict.Count : transactionCount;
                res.AddRange(dict.Values.Take(take)
                    // .OrderBy(x => x.EnqueueTime)
                    .Select(x => x.Transaction));
                
                transactionCount -= take;
            }

            return res;
        }

        private int GetTxCount()
        {
            return _dictList.Sum(dict => dict.Count);
        }

        public async Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            if (_bestChainHash == Hash.Empty)
                return;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            await _transactionManager.AddTransactionsAsync(transactions.ToList());
            stopwatch.Stop();
            Logger.LogDebug($"Add {transactions.Count()} tx elapsed {stopwatch.ElapsedMilliseconds}");

            foreach (var transaction in transactions)
            {
                var transactionId = transaction.GetHash();
                var queuedTransaction = new QueuedTransaction
                {
                    TransactionId = transactionId,
                    Transaction = transaction,
                    EnqueueTime = TimestampHelper.GetUtcNow()
                };
                // var sendResult = await AcceptTransactionAsync(queuedTransaction);
                var sendResult = await _actionBlock.SendAsync(queuedTransaction);
                if (sendResult) continue;
                await LocalEventBus.PublishAsync(new TransactionValidationStatusChangedEvent
                {
                    TransactionId = transactionId,
                    TransactionResultStatus = TransactionResultStatus.NodeValidationFailed,
                    Error = "Failed to enter tx hub."
                });
                Logger.LogWarning($"Process transaction:{queuedTransaction.TransactionId} failed.");
            }
        }

        public async Task UpdateTransactionPoolByBestChainAsync(Hash bestChainHash, long bestChainHeight)
        {
            // var minimumHeight = _allTransactions.Count == 0
            //     ? 0
            //     : _allTransactions.Min(kv => kv.Value.Transaction.RefBlockNumber);
            // var prefixes = await GetPrefixesByHeightAsync(minimumHeight, bestChainHash, bestChainHeight);
            // ResetCurrentCollections();
            // var dict = _dictList[(int) bestChainHeight % _transactionOptions.PoolParallelismDegree];
            // foreach (var queuedTransaction in dict.Values)
            // {
            //     // prefixes.TryGetValue(queuedTransaction.Transaction.RefBlockNumber, out var prefix);
            //     queuedTransaction.RefBlockStatus =
            //         CheckRefBlockStatus(queuedTransaction.Transaction, bestChainHeight);
            //     AddToCollection(queuedTransaction);
            // }
            //
            // CleanTransactions(_expiredByExpiryBlock, bestChainHeight);

            _bestChainHash = bestChainHash;
            _bestChainHeight = bestChainHeight;
        }

        public Task CleanByHeightAsync(long height)
        {
            // CleanTransactions(_expiredByExpiryBlock, height);
            // CleanTransactions(_invalidatedByBlock, height);

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
                AllTransactionCount = GetTxCount(),
                // ValidatedTransactionCount = _validatedTransactions.Count
            });
        }

        public Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId)
        {
            var dict = GetDict(transactionId);
            dict.TryGetValue(transactionId, out var receipt);
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

        // private static RefBlockStatus CheckRefBlockStatus(Transaction transaction, ByteString prefix,
        //     long bestChainHeight)
        // {
        //     if (transaction.GetExpiryBlockNumber() <= bestChainHeight)
        //     {
        //         return RefBlockStatus.RefBlockExpired;
        //     }
        //
        //     return transaction.RefBlockPrefix == prefix ? RefBlockStatus.RefBlockValid : RefBlockStatus.RefBlockInvalid;
        // }
        
        private static RefBlockStatus CheckRefBlockStatus(Transaction transaction, long bestChainHeight)
        {
            // if (transaction.GetExpiryBlockNumber() <= bestChainHeight)
            // {
            //     return RefBlockStatus.RefBlockExpired;
            // }

            // return transaction.RefBlockPrefix == prefix ? RefBlockStatus.RefBlockValid : RefBlockStatus.RefBlockInvalid;
            return RefBlockStatus.RefBlockValid;
        }

        private ByteString GetPrefixByHash(Hash hash)
        {
            return hash == null ? null : BlockHelper.GetRefBlockPrefix(hash);
        }

        // private async Task<ByteString> GetPrefixByHeightAsync(long height, Hash bestChainHash)
        // {
        //     var chain = await _blockchainService.GetChainAsync();
        //     var hash = await _blockchainService.GetBlockHashByHeightAsync(chain, height, bestChainHash);
        //     return GetPrefixByHash(hash);
        // }

        // private async Task<Dictionary<long, ByteString>> GetPrefixesByHeightAsync(long firstHeight, Hash bestChainHash,
        //     long bestChainHeight)
        // {
        //     var blockIndexes =
        //         await _blockchainService.GetBlockIndexesAsync(firstHeight, bestChainHash, bestChainHeight);
        //
        //     return blockIndexes.ToDictionary(blockIndex => blockIndex.BlockHeight,
        //         blockIndex => GetPrefixByHash(blockIndex.BlockHash));
        // }

        private void ResetCurrentCollections()
        {
            _expiredByExpiryBlock = new ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>();
            // _invalidatedByBlock = new ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>();
            // _validatedTransactions = new ConcurrentDictionary<Hash, QueuedTransaction>();
        }

        private void AddToCollection(QueuedTransaction queuedTransaction)
        {
            switch (queuedTransaction.RefBlockStatus)
            {
                case RefBlockStatus.RefBlockExpired:
                    AddToCollection(_expiredByExpiryBlock, queuedTransaction);
                    break;
                // case RefBlockStatus.RefBlockInvalid:
                //     AddToCollection(_invalidatedByBlock, queuedTransaction);
                //     break;
                // case RefBlockStatus.RefBlockValid:
                //     _validatedTransactions.TryAdd(queuedTransaction.TransactionId, queuedTransaction);
                //     break;
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
                var dict = GetDict(transactionId);
                dict.TryRemove(transactionId, out _);
            }
        }

        #endregion

        #region Data flow

        private ActionBlock<QueuedTransaction> CreateQueuedTransactionBufferBlock()
        {
            var validationTransformBlock = new ActionBlock<QueuedTransaction>(
                async queuedTransaction =>
                    await ProcessQueuedTransactionAsync(queuedTransaction, AcceptTransactionAsync),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = Math.Max(_transactionOptions.ActionBlockCapacity, 1), // cannot be zero
                    EnsureOrdered = false,
                    MaxDegreeOfParallelism = _transactionOptions.PoolParallelismDegree
                });

            return validationTransformBlock;
        }


        private async Task<bool> VerifyTransactionAcceptableAsync(QueuedTransaction queuedTransaction,
            ConcurrentDictionary<Hash, QueuedTransaction> dict)
        {
            if (dict.ContainsKey(queuedTransaction.TransactionId))
            {
                return false;
            }

            if (!queuedTransaction.Transaction.VerifyExpiration(_bestChainHeight))
            {
                //await PublishTransactionNodeValidationFailedEventAsync(queuedTransaction.TransactionId,
                //    $"Transaction expired.Transaction RefBlockNumber is {queuedTransaction.Transaction.RefBlockNumber},best chain height is {_bestChainHeight}");
                return false;
            }

            if (dict.Count >= _transactionOptions.PoolLimit / _transactionOptions.PoolParallelismDegree)
            {
                //await PublishTransactionNodeValidationFailedEventAsync(queuedTransaction.TransactionId,
                //    "Transaction Pool is full.");
                return false;
            }
            //
            // if (await _blockchainService.HasTransactionAsync(queuedTransaction.TransactionId))
            // {
            //     return false;
            // }

            return true;
        }

        private ConcurrentDictionary<Hash, QueuedTransaction> GetDict(Hash txId)
        {
            int index = Math.Abs((int) txId.ToInt64()) % _transactionOptions.PoolParallelismDegree;
            return _dictList[index];
        }

        private async Task<QueuedTransaction> AcceptTransactionAsync(QueuedTransaction queuedTransaction)
        {
            var dict = GetDict(queuedTransaction.TransactionId);
            if (!await VerifyTransactionAcceptableAsync(queuedTransaction, dict))
            {
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
                Logger.LogDebug($"Transaction {queuedTransaction.TransactionId} validation failed.");
                return null;
            }

            // double check
            // var hasTransaction = await _blockchainService.HasTransactionAsync(queuedTransaction.TransactionId);
            // if (hasTransaction)
            //     return null;

            var addSuccess = dict.TryAdd(queuedTransaction.TransactionId, queuedTransaction);
            if (addSuccess)
            {
                await UpdateQueuedTransactionRefBlockStatusAsync(queuedTransaction);
                return queuedTransaction;
            }

            Logger.LogWarning($"Transaction {queuedTransaction.TransactionId} insert failed.");
            return null;
        }

        private async Task PublishTransactionNodeValidationFailedEventAsync(Hash transactionId, string error)
        {
            await LocalEventBus.PublishAsync(new TransactionValidationStatusChangedEvent
            {
                TransactionId = transactionId,
                TransactionResultStatus = TransactionResultStatus.NodeValidationFailed,
                Error = error
            });
        }

        private async Task<QueuedTransaction> UpdateQueuedTransactionRefBlockStatusAsync(
            QueuedTransaction queuedTransaction)
        {
            await LocalEventBus.PublishAsync(new TransactionAcceptedEvent
            {
                Transaction = queuedTransaction.Transaction
            });

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