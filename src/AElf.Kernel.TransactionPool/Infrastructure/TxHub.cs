using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContractExecution.Application;
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

        private readonly ActionBlock<QueuedTransaction> _processTransactionJobs;

        private long _bestChainHeight = Constants.GenesisBlockHeight - 1;
        private Hash _bestChainHash = Hash.Empty;

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
            _processTransactionJobs = new ActionBlock<QueuedTransaction>(ProcessTransactionAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 1
                });
        }

        public async Task<ExecutableTransactionSet> GetExecutableTransactionSetAsync(int transactionCount = 0)
        {
            var output = new ExecutableTransactionSet
            {
                PreviousBlockHash = _bestChainHash,
                PreviousBlockHeight = _bestChainHeight
            };

            if (transactionCount == -1)
            {
                return output;
            }

            var chain = await _blockchainService.GetChainAsync();
            if (chain.BestChainHash != _bestChainHash)
            {
                Logger.LogWarning(
                    $"Attempting to retrieve executable transactions while best chain records don't match.");
                return output;
            }

            output.Transactions.AddRange(_validatedTransactions.Values.OrderBy(x => x.EnqueueTime)
                .Where((x, i) => transactionCount <= 0 || i < transactionCount).Select(x => x.Transaction));

            return output;
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

        private static void UpdateRefBlockStatus(QueuedTransaction queuedTransaction, ByteString prefix,
            long bestChainHeight)
        {
            if (queuedTransaction.Transaction.GetExpiryBlockNumber() <= bestChainHeight)
            {
                queuedTransaction.RefBlockStatus = RefBlockStatus.RefBlockExpired;
                return;
            }

            if (queuedTransaction.Transaction.RefBlockPrefix == prefix)
            {
                queuedTransaction.RefBlockStatus = RefBlockStatus.RefBlockValid;
                return;
            }

            queuedTransaction.RefBlockStatus = RefBlockStatus.RefBlockInvalid;
        }

        private ByteString GetPrefixByHash(Hash hash)
        {
            return hash == null ? null : ByteString.CopyFrom(hash.ToByteArray().Take(4).ToArray());
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

        #region Event Handler Methods

        public async Task HandleTransactionsReceivedAsync(TransactionsReceivedEvent eventData)
        {
            if (_bestChainHash == Hash.Empty)
                return;

            foreach (var transaction in eventData.Transactions)
            {
                if (_processTransactionJobs.InputCount > _transactionOptions.PoolLimit)
                {
                    Logger.LogWarning("Already too many transaction processing job enqueued.");
                    break;
                }

                var queuedTransaction = new QueuedTransaction
                {
                    TransactionId = transaction.GetHash(),
                    Transaction = transaction,
                    EnqueueTime = TimestampHelper.GetUtcNow()
                };
                await _processTransactionJobs.SendAsync(queuedTransaction);
            }
        }

        private async Task ProcessTransactionAsync(QueuedTransaction queuedTransaction)
        {
            if (_allTransactions.Count > _transactionOptions.PoolLimit)
            {
                Logger.LogWarning("Tx pool is full.");
                return;
            }

            if (_allTransactions.ContainsKey(queuedTransaction.TransactionId))
                return;

            if (!queuedTransaction.Transaction.VerifyExpiration(_bestChainHeight))
            {
                Logger.LogWarning($"Transaction {queuedTransaction.TransactionId} already expired.");
                return;
            }

            var validationResult =
                await _transactionValidationService.ValidateTransactionWhileCollectingAsync(queuedTransaction.Transaction);
            if (!validationResult)
            {
                Logger.LogWarning($"Transaction {queuedTransaction.TransactionId} validation failed.");
                return;
            }

            var hasTransaction = await _blockchainService.HasTransactionAsync(queuedTransaction.TransactionId);
            if (hasTransaction)
                return;

            await _transactionManager.AddTransactionAsync(queuedTransaction.Transaction);
            var addSuccess = _allTransactions.TryAdd(queuedTransaction.TransactionId, queuedTransaction);
            if (!addSuccess)
            {
                Logger.LogWarning($"Transaction {queuedTransaction.TransactionId} insert failed.");
                return;
            }

            var prefix = await GetPrefixByHeightAsync(queuedTransaction.Transaction.RefBlockNumber, _bestChainHash);
            UpdateRefBlockStatus(queuedTransaction, prefix, _bestChainHeight);

            if (queuedTransaction.RefBlockStatus == RefBlockStatus.RefBlockExpired)
                return;

            if (queuedTransaction.RefBlockStatus == RefBlockStatus.RefBlockValid)
            {
                await LocalEventBus.PublishAsync(new TransactionAcceptedEvent()
                {
                    Transaction = queuedTransaction.Transaction
                });
            }
        }

        public Task HandleBlockAcceptedAsync(BlockAcceptedEvent eventData)
        {
            CleanTransactions(eventData.Block.Body.TransactionIds.ToList());
            
            return Task.CompletedTask;
        }

        public async Task HandleBestChainFoundAsync(BestChainFoundEventData eventData)
        {
            Logger.LogTrace(
                $"Handle best chain found: BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");

            var minimumHeight = _allTransactions.Count == 0
                ? 0
                : _allTransactions.Min(kv => kv.Value.Transaction.RefBlockNumber);
            var prefixes = await GetPrefixesByHeightAsync(minimumHeight, eventData.BlockHash, eventData.BlockHeight);
            ResetCurrentCollections();
            foreach (var queuedTransaction in _allTransactions.Values)
            {
                prefixes.TryGetValue(queuedTransaction.Transaction.RefBlockNumber, out var prefix);
                UpdateRefBlockStatus(queuedTransaction, prefix, eventData.BlockHeight);
                AddToCollection(queuedTransaction);
            }

            CleanTransactions(_expiredByExpiryBlock, eventData.BlockHeight);

            _bestChainHash = eventData.BlockHash;
            _bestChainHeight = eventData.BlockHeight;

            Logger.LogTrace(
                $"Finish handle best chain found: BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");
        }

        public async Task HandleNewIrreversibleBlockFoundAsync(NewIrreversibleBlockFoundEvent eventData)
        {
            CleanTransactions(_expiredByExpiryBlock, eventData.BlockHeight);
            CleanTransactions(_invalidatedByBlock, eventData.BlockHeight);

            await Task.CompletedTask;
        }

        public async Task HandleUnexecutableTransactionsFoundAsync(UnexecutableTransactionsFoundEvent eventData)
        {
            CleanTransactions(eventData.Transactions);

            await Task.CompletedTask;
        }

        #endregion

        public Task<int> GetAllTransactionCountAsync()
        {
            return Task.FromResult(_allTransactions.Count);
        }

        public Task<int> GetValidatedTransactionCountAsync()
        {
            return Task.FromResult(_validatedTransactions.Count);
        }

        public async Task<bool> IsTransactionExistsAsync(Hash transactionId)
        {
            return await _transactionManager.HasTransactionAsync(transactionId);
        }
    }
}