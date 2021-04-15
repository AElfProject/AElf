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

        private readonly ConcurrentDictionary<Address, ConcurrentDictionary<Hash, QueuedTransaction>> _groupedTxList =
            new ConcurrentDictionary<Address, ConcurrentDictionary<Hash, QueuedTransaction>>();

        private readonly ConcurrentDictionary<Hash, QueuedTransaction> _txList =
            new ConcurrentDictionary<Hash, QueuedTransaction>();

        private ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>> _expiredByExpiryBlock =
            new ConcurrentDictionary<long, ConcurrentDictionary<Hash, QueuedTransaction>>();
        
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
            LocalEventBus = NullLocalEventBus.Instance;
            _transactionOptions = transactionOptions.Value;
            _actionBlock = CreateQueuedTransactionBufferBlock();
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

            List<Transaction> list = GetTransactions(transactionCount);
            output.Transactions.AddRange(list);
            Logger.LogTrace("End TxHub.GetExecutableTransactionSetAsync");
            return output;
        }

        private List<Transaction> GetTransactions(int transactionCount)
        {
            var res = new List<Transaction>();

            var currentGroup = _groupedTxList.Count;
            if (transactionCount == 0 || currentGroup == 0)
                return res;
            var count = transactionCount / currentGroup;
            foreach (var dict in _groupedTxList)
            {
                var countPerGroup = dict.Value.Count;
                var take = countPerGroup < count ? countPerGroup : count;
                res.AddRange(dict.Value.Take(take)
                    .Select(x => x.Value.Transaction));
            }

            return res;
        }

        private int GetTxCount()
        {
            return _groupedTxList.Values.Sum(dict => dict.Count);
        }

        public async Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            if (_bestChainHash == Hash.Empty)
                return;
            
            await _transactionManager.AddTransactionsAsync(transactions.ToList());

            foreach (var transaction in transactions)
            {
                var transactionId = transaction.GetHash();
                var queuedTransaction = new QueuedTransaction
                {
                    TransactionId = transactionId,
                    Transaction = transaction,
                    EnqueueTime = TimestampHelper.GetUtcNow()
                };
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
            _bestChainHash = bestChainHash;
            _bestChainHeight = bestChainHeight;
        }

        public Task CleanByHeightAsync(long height)
        {
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
            });
        }

        public Task<QueuedTransaction> GetQueuedTransactionAsync(Hash transactionId)
        {
            _txList.TryGetValue(transactionId, out var receipt);
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

        private void CleanTransactions(IEnumerable<Hash> transactionIds)
        {
            Logger.LogDebug("Begin clean transactions");
            
            foreach (var transactionId in transactionIds)
            {
                if (_txList.TryRemove(transactionId, out var queuedTransaction))
                {
                    var group = _groupedTxList[queuedTransaction.Transaction.From];
                    group.TryRemove(queuedTransaction.TransactionId, out _);

                    if (group.IsEmpty)
                    {
                        _groupedTxList.TryRemove(queuedTransaction.Transaction.From, out _);
                    }
                }
            }
            
            Logger.LogDebug("End clean transactions");
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


        private async Task<bool> VerifyTransactionAcceptableAsync(QueuedTransaction queuedTransaction)
        {
            if (_txList.ContainsKey(queuedTransaction.TransactionId))
            {
                return false;
            }

            // if (!queuedTransaction.Transaction.VerifyExpiration(_bestChainHeight))
            // {
            //     //await PublishTransactionNodeValidationFailedEventAsync(queuedTransaction.TransactionId,
            //     //    $"Transaction expired.Transaction RefBlockNumber is {queuedTransaction.Transaction.RefBlockNumber},best chain height is {_bestChainHeight}");
            //     return false;
            // }

            // if (_txList.Count >= _transactionOptions.PoolLimit)
            // {
            //     //await PublishTransactionNodeValidationFailedEventAsync(queuedTransaction.TransactionId,
            //     //    "Transaction Pool is full.");
            //     return false;
            // }
            //
            // if (await _blockchainService.HasTransactionAsync(queuedTransaction.TransactionId))
            // {
            //     return false;
            // }

            return true;
        }

        private async Task<QueuedTransaction> AcceptTransactionAsync(QueuedTransaction queuedTransaction)
        {
            if (!await VerifyTransactionAcceptableAsync(queuedTransaction))
            {
                return null;
            }

            // var validationResult =
            //     await _transactionValidationService.ValidateTransactionWhileCollectingAsync(new ChainContext
            //     {
            //         BlockHash = _bestChainHash,
            //         BlockHeight = _bestChainHeight
            //     }, queuedTransaction.Transaction);
            // if (!validationResult)
            // {
            //     Logger.LogDebug($"Transaction {queuedTransaction.TransactionId} validation failed.");
            //     return null;
            // }

            _txList.TryAdd(queuedTransaction.TransactionId, queuedTransaction);
            if (!_groupedTxList.TryGetValue(queuedTransaction.Transaction.From, out var groupedList))
            {
                groupedList = new ConcurrentDictionary<Hash, QueuedTransaction>();
                _groupedTxList[queuedTransaction.Transaction.From] = groupedList;
            }

            var addSuccess = groupedList.TryAdd(queuedTransaction.TransactionId, queuedTransaction);
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