using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Akka.Dispatch;
using Easy.MessageHub;
using NLog;

namespace AElf.ChainController.TxMemPool
{
    public class TxPoolServiceBM : ITxPoolService
    {
        private readonly ILogger _logger;
        private readonly IChainService _chainService;
        private readonly ITxValidator _txValidator;
        private readonly ITransactionManager _transactionManager;
        private ulong Least { get; set; }
        private ulong Limit { get; set; }

        public TxPoolServiceBM(ILogger logger, IChainService chainService, ITxValidator txValidator,
            ITransactionManager transactionManager)
        {
            _logger = logger;
            _chainService = chainService;
            _txValidator = txValidator;
            _transactionManager = transactionManager;
        }

        public void Start()
        {
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        private readonly ConcurrentDictionary<Hash, Transaction> _contractTxs =
            new ConcurrentDictionary<Hash, Transaction>();

        private readonly ConcurrentDictionary<Hash, Transaction> _dPoSTxs =
            new ConcurrentDictionary<Hash, Transaction>();

        private readonly ConcurrentBag<Hash> _bpAddrs = new ConcurrentBag<Hash>();

        /// <inheritdoc/>
        public async Task<TxValidation.TxInsertionAndBroadcastingError> AddTxAsync(Transaction tx)
        {
            var txExecuted = await _transactionManager.GetTransaction(tx.GetHash());
            if (txExecuted != null)
            {
                return TxValidation.TxInsertionAndBroadcastingError.AlreadyExecuted;
            }

            var res = await AddTransaction(tx);
            if (res == TxValidation.TxInsertionAndBroadcastingError.Success)
            {
                MessageHub.Instance.Publish(new TransactionAddedToPool(tx));
            }

            return res;
        }

        /// <summary>
        /// enqueue tx in pool
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private async Task<TxValidation.TxInsertionAndBroadcastingError> AddTransaction(Transaction tx)
        {
            var res = _txValidator.ValidateTx(tx);
            if (res != TxValidation.TxInsertionAndBroadcastingError.Valid)
            {
                return res;
            }

            res = await _txValidator.ValidateReferenceBlockAsync(tx);
            if (res != TxValidation.TxInsertionAndBroadcastingError.Valid)
            {
                return res;
            }

            if (tx.Type == TransactionType.DposTransaction)
            {
                return await Task.FromResult(AddDPoSTransaction(tx));
            }

            return await Task.FromResult(AddContractTransaction(tx));
        }

        /// <summary>
        /// enqueue dpos tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private TxValidation.TxInsertionAndBroadcastingError AddDPoSTransaction(Transaction tx)
        {
            if (tx.Type != TransactionType.DposTransaction) return TxValidation.TxInsertionAndBroadcastingError.Failed;
            if (_dPoSTxs.ContainsKey(tx.GetHash()))
                return TxValidation.TxInsertionAndBroadcastingError.AlreadyInserted;

            if (_dPoSTxs.TryAdd(tx.GetHash(), tx))
            {
                _bpAddrs.Add(tx.From);
                return TxValidation.TxInsertionAndBroadcastingError.Success;
            }

            return TxValidation.TxInsertionAndBroadcastingError.Failed;
        }

        /// <summary>
        /// enqueue contract tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private TxValidation.TxInsertionAndBroadcastingError AddContractTransaction(Transaction tx)
        {
            if (tx.Type != TransactionType.ContractTransaction)
                return TxValidation.TxInsertionAndBroadcastingError.Failed;
            if (_contractTxs.ContainsKey(tx.GetHash()))
                return TxValidation.TxInsertionAndBroadcastingError.AlreadyInserted;

            if (_contractTxs.TryAdd(tx.GetHash(), tx))
            {
                return TxValidation.TxInsertionAndBroadcastingError.Success;
            }

            return TxValidation.TxInsertionAndBroadcastingError.Failed;
        }

        /// <inheritdoc/>
        public async Task RollBack(List<Transaction> txsOut)
        {
            foreach (var tx in txsOut)
            {
                if (tx.Type == TransactionType.DposTransaction)
                {
                    AddDPoSTransaction(tx);
                }
                else
                {
                    AddContractTransaction(tx);
                }

                tx.Unclaim();
            }

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool TryGetTx(Hash txHash, out Transaction tx)
        {
            return _contractTxs.TryGetValue(txHash, out tx) || _dPoSTxs.TryGetValue(txHash, out tx);
        }

        public List<Hash> GetMissingTransactions(IBlock block)
        {
            var res = new List<Hash>();
            var txs = block.Body.Transactions;
            foreach (var id in txs)
            {
                if (!TryGetTx(id, out _))
                {
                    res.Add(id);
                }
            }

            return res;
        }

        /// <inheritdoc/>
        public void RemoveAsync(Hash txHash)
        {
            if (_dPoSTxs.TryRemove(txHash, out _))
                return;
            _contractTxs.TryRemove(txHash, out _);
        }

        /// <inheritdoc/>
        public async Task<List<Transaction>> GetReadyTxsAsync(double intervals = 150)
        {
            // TODO: Improve performance
            var txs = _dPoSTxs.Values.ToList();
            _logger.Debug($"Got {txs.Count} DPoS tx");
            if ((ulong) _contractTxs.Count < Least)
            {
                _logger.Debug($"Regular txs {Least} required, but we only have {_contractTxs.Count}");
                return txs;
            }

            var invalid = new List<Hash>();
            foreach (var kv in _contractTxs)
            {
                if (Limit != 0 && (ulong) txs.Count > Limit)
                {
                    continue;
                }

                if (!kv.Value.Claim())
                {
                    continue;
                }

                var res = await _txValidator.ValidateReferenceBlockAsync(kv.Value);
                if (res != TxValidation.TxInsertionAndBroadcastingError.Valid)
                {
                    invalid.Add(kv.Key);
                }
                else
                {
                    txs.Add(kv.Value);
                }
            }

            foreach (var hash in invalid)
            {
                _contractTxs.TryRemove(hash, out _);
            }

            _logger.Debug($"Got {txs.Count} total tx");
            return txs;
        }

        /// <inheritdoc/>
        public async Task UpdateAccountContext(HashSet<Hash> addrs)
        {
        }

        public void SetBlockVolume(ulong minimal, ulong maximal)
        {
            Least = minimal;
            Limit = maximal;
        }

        /// <inheritdoc/>
        public Task RemoveTxWithWorstFeeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ulong> GetPoolSize()
        {
            return Task.FromResult((ulong) _contractTxs.Count);
        }
    }
}