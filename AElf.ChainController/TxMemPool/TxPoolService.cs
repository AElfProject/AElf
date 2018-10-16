using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.ChainController.TxMemPool;
using AElf.Common.Attributes;
using AElf.Common.Synchronisation;
using AElf.Common;
using AElf.Kernel;
using AElf.SmartContract;
using Easy.MessageHub;
using NLog;
using NServiceKit.Common.Extensions;
using ReaderWriterLock = AElf.Common.Synchronisation.ReaderWriterLock;

namespace AElf.ChainController.TxMemPool
{
    [LoggerName("Txpool")]
    public class TxPoolService : ITxPoolService
    {
        private readonly IContractTxPool _contractTxPool;
        private readonly IPriorTxPool _priorTxPool;
        private readonly IAccountContextService _accountContextService;
        private readonly ILogger _logger;

        public TxPoolService(IContractTxPool contractTxPool, IAccountContextService accountContextService, 
            ILogger logger, IPriorTxPool priorTxPool)
        {
            _contractTxPool = contractTxPool;
            _accountContextService = accountContextService;
            _logger = logger;
            _priorTxPool = priorTxPool;
        }

        /// <summary>
        /// Signals to a CancellationToken that it should be canceled
        /// </summary>
        private CancellationTokenSource Cts { get; set; }

        private TxPoolSchedulerLock ContractTxLock { get; } = new TxPoolSchedulerLock();
        private TxPoolSchedulerLock PriorTxLock { get; } = new TxPoolSchedulerLock();


        private readonly ConcurrentDictionary<Hash, Transaction> _contractTxs = new ConcurrentDictionary<Hash, Transaction>();
        private readonly ConcurrentDictionary<Hash, Transaction> _priorTxs = new ConcurrentDictionary<Hash, Transaction>();
        private readonly ConcurrentBag<Address> _priorAddresses = new ConcurrentBag<Address>();

        /// <inheritdoc/>
        public async Task<TxValidation.TxInsertionAndBroadcastingError> AddTxAsync(Transaction tx, bool validateReference = true)
        {
            if (Cts.IsCancellationRequested) 
                return TxValidation.TxInsertionAndBroadcastingError.PoolClosed;
            
            return await AddTransaction(tx);
        }

        /// <summary>
        /// enqueue tx in pool
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private async Task<TxValidation.TxInsertionAndBroadcastingError> AddTransaction(Transaction tx)
        {
            if (tx.Type == TransactionType.DposTransaction)
            {
                await TrySetNonce(tx.From, TransactionType.DposTransaction);
                return await PriorTxLock.WriteLock(() => AddPriorTransaction(tx));
            }

            await TrySetNonce(tx.From, TransactionType.ContractTransaction);
            return await ContractTxLock.WriteLock(() => AddContractTransaction(tx));
        }
        
        /// <summary>
        /// enqueue prior tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private TxValidation.TxInsertionAndBroadcastingError AddPriorTransaction(Transaction tx)
        {
            if (tx.Type != TransactionType.DposTransaction) return TxValidation.TxInsertionAndBroadcastingError.Failed;
            if (_priorTxs.ContainsKey(tx.GetHash()))
                return TxValidation.TxInsertionAndBroadcastingError.AlreadyInserted;
            
            var res = _priorTxPool.EnQueueTx(tx);
            if (res == TxValidation.TxInsertionAndBroadcastingError.Success)
            {
                // add tx
                _priorTxs.GetOrAdd(tx.GetHash(), tx);
                _priorAddresses.Add(tx.From);
            }
            return res;
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
            var res = _contractTxPool.EnQueueTx(tx);
            if (res == TxValidation.TxInsertionAndBroadcastingError.Success)
            {
                // add tx
                _contractTxs.GetOrAdd(tx.GetHash(), tx);
            }
            return res;
        }
        
        /// <summary>
        /// set nonce for address in tx pool
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private async Task TrySetNonce(Address addr, TransactionType type)
        {
            IPool pool;
            if(type == TransactionType.ContractTransaction)
                pool =  _contractTxPool;
            else
                pool = _priorTxPool;
            // tx from account state
            if (!pool.GetNonce(addr).HasValue)
            {
                var incrementId = (await _accountContextService.GetAccountDataContext(addr, pool.ChainId))
                    .IncrementId;
                pool.TrySetNonce(addr, incrementId);
            }
        }

        /// <inheritdoc/>
        public void RemoveAsync(Hash txHash)
        {
            if (_priorTxs.TryRemove(txHash, out _))
                return;
            _contractTxs.TryRemove(txHash, out _);
        }

        /// <inheritdoc/>
        public Task RemoveTxWithWorstFeeAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<Transaction>> GetReadyTxsAsync(Round currentRoundInfo, double intervals = 150)
        {
            // get prior tx
            var prior = await PriorTxLock.WriteLock(() =>
            {
                var readyTxs = _priorTxPool.ReadyTxs();
                foreach (var tx in readyTxs)
                {
                    _priorTxs.TryRemove(tx.GetHash(), out _);
                }
                _logger.Log(LogLevel.Debug, $"Got {readyTxs.Count} prior tx(s)");
                return readyTxs;
            });
            
             List<Transaction> contractTxs = null;
            bool available = false;
            bool complete = false;
            long count = -1;
            using (var tokenSource = new CancellationTokenSource())
            {
                intervals = Math.Max(intervals, 150);
                
                var token = tokenSource.Token;
                var t = ContractTxLock.WriteLock(() =>
                {
                    if (token.IsCancellationRequested)
                    {
                        //case 2
                        _logger.Log(LogLevel.Debug, "TIMEOUT! - No time left to get txs.");
                        return;
                    }
                
                    count = (long) _contractTxPool.GetExecutableSize();
                    if (count < (long) _contractTxPool.Least)
                    {
                        // case 3
                        return;
                    }
                    if (token.IsCancellationRequested)
                    {
                        //case 2
                        return;
                    }
                    available = true;
                    contractTxs = _contractTxPool.ReadyTxs(); //case 1
                    complete = true;
                }, token);
                
                try
                {
                    bool res = t.Wait(TimeSpan.FromMilliseconds(intervals));
                    if(!res)
                        tokenSource.Cancel();
                    // NOTE: be careful, some txs maybe lost here without this
                    if (available && !complete)
                    {
                        _logger.Log(LogLevel.Debug, "Be careful! it takes more time to get txs!");
                        t.Wait();
                    }
                        
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerExceptions.Any(e => e is TaskCanceledException))
                        _logger.Log(LogLevel.Debug, "Exception: " + ae.Message);
                    else
                        throw;
                }
                finally
                {
                    tokenSource.Dispose();
                }
            }
            
            if (complete)
            {
                // case 1
                // get txs successfully
                prior.AddRange(contractTxs);
            
                foreach (var tx in contractTxs)
                {
                    _contractTxs.TryRemove(tx.GetHash(), out _);
                }
                _logger.Log(LogLevel.Debug, $"Totally {count} txs in pool, got {contractTxs.Count}. ");
            }
            else if(available)
            {
                // something is wrong which should not happen
                _logger.Log(LogLevel.Error, "FAILED to get all transactions，some would be lost!");
            }
            else if (count == -1)
            {
                // case 3
                _logger.Log(LogLevel.Debug, "TIMEOUT! - Unable to count txs.");
            }
            else if(count < (long) _contractTxPool.Least)
            {
                // case 3
                _logger.Log(LogLevel.Debug,
                    $"Only {count} Contract tx(s) in pool are ready: less than {_contractTxPool.Least}.");
            }
            else
            {
                // only few cases.
                _logger.Log(LogLevel.Debug, "TIMEOUT! - Enough txs but no time left to get txs.");
            }
                
            return prior;
        }

        /// <summary>
        /// persist txs with storage
        /// </summary>
        /// <param name="txs"></param>
        /// <returns></returns>
        private void PersistTxs(IEnumerable<Transaction> txs)
        {
            foreach (var t in txs)
            {
                // TODO: persist tx
                //await _transactionManager.AddTransactionAsync(tx);
            }
        }

        public List<Transaction> GetSystemTxs()
        {
            return _priorTxs.Values.ToList();
        }

        /// <inheritdoc/>
        public Task<bool> GetReadyTxsAsync(Address addr, ulong start, ulong ids)
        {
            return _priorAddresses.Contains(addr)
                ? PriorTxLock.WriteLock(() => { return _priorTxPool.ReadyTxs(addr, start, ids); })
                : ContractTxLock.WriteLock(() => { return _contractTxPool.ReadyTxs(addr, start, ids); });
        }

        /// <inheritdoc/>
        public async Task<ulong> GetPoolSize()
        {
            /*lock (this)
            {
                return Task.FromResult(_contractTxPool.Size);
            }*/

            return await ContractTxLock.ReadLock(() => _contractTxPool.Size) +
                   await PriorTxLock.ReadLock(() => _priorTxPool.Size);
        }

        /// <inheritdoc/>
        public bool TryGetTx(Hash txHash, out Transaction tx)
        {
            return _contractTxs.TryGetValue(txHash, out tx) || _priorTxs.TryGetValue(txHash, out tx);
        }
        
        public List<Hash> GetMissingTransactions(IBlock block)
        {
            try
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
            catch (Exception e)
            {
                _logger?.Trace("Error while getting missing transactions");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<ulong> GetWaitingSizeAsync()
        {
            return await ContractTxLock.ReadLock(() => _contractTxPool.GetWaitingSize()) +
                   await PriorTxLock.ReadLock(() => _priorTxPool.GetWaitingSize());
        }

        /// <inheritdoc/>
        public async Task<ulong> GetExecutableSizeAsync()
        {
            return await ContractTxLock.ReadLock(() => _contractTxPool.GetExecutableSize()) +
                   await PriorTxLock.ReadLock(() => _priorTxPool.GetExecutableSize());
        }

        /// <inheritdoc/>
        public async Task UpdateAccountContext(HashSet<Address> addrs)
        {
            foreach (var addr in addrs)
            {
                IPool pool;
                if (!_priorAddresses.Contains(addr))
                    pool = _contractTxPool;
                else
                {
                    pool = _priorTxPool;
                }
                var id = pool.GetNonce(addr);
                if(!id.HasValue)
                    continue;
                
                // update account context
                await _accountContextService.SetAccountContext(new AccountDataContext
                {
                    IncrementId = id.Value,
                    Address = addr,
                    ChainId = pool.ChainId
                });
            }
        }

        /// <inheritdoc/>
        public void Start()
        {
            // TODO: more initialization
            Cts = new CancellationTokenSource();
        }

        /// <inheritdoc/>
        public Task Stop()
        {
            /*await ContractTxLock.WriteLock(() =>
            {
                // TODO: release resources
                Cts.Cancel();
                Cts.Dispose();
                //EnqueueEvent.Dispose();
                //DemoteEvent.Dispose();
            });*/
            return ContractTxLock.WriteLock(() =>
            {
                Cts.Cancel();
                Cts.Dispose();
            });
        }

        /// <inheritdoc/>
        public ulong GetIncrementId(Address addr, bool isBlockProducer = false)
        {
            ILock @lock;
            IPool pool;
            if (!isBlockProducer)
            {
                pool = _contractTxPool;
            }
            else
            {
                pool = _priorTxPool;
            }
            return pool.GetPendingIncrementId(addr);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// only contract txs would be reverted
        /// </remarks>
        public async Task Revert(List<Transaction> txsOut)
        {
            _logger?.Log(LogLevel.Debug, "Revert {0} txs ...", txsOut.Count);

            try
            {
                var nonces = txsOut.Select(async p => await TrySetNonce(p.From, p.Type));
                await Task.WhenAll(nonces);

                var tmap = txsOut.Aggregate(new Dictionary<Address, HashSet<Transaction>>(),  (current, p) =>
                {
                    if (!current.TryGetValue(p.From, out _))
                    {
                        current[p.From] = new HashSet<Transaction>();
                    }

                    current[p.From].Add(p);
                    return current;
                });
                
                foreach (var kv in tmap)
                {
                    
                    ILock @lock;
                    IPool pool;
                    if (!_priorAddresses.Contains(kv.Key))
                    {
                        @lock = ContractTxLock;
                        pool = _contractTxPool;
                    }
                    else
                    {
                        @lock = PriorTxLock;
                        pool = _priorTxPool;
                    }
                    var nonce = pool.GetNonce(kv.Key);
                    var min = kv.Value.Min(t => t.IncrementId);
                    var n = nonce ?? (await _accountContextService.GetAccountDataContext(kv.Key, pool.ChainId))
                            .IncrementId;
                    
                    // cannot be reverted
                    if(min >= n)
                        continue;
                    
                    await _accountContextService.SetAccountContext(new AccountDataContext
                    {
                        IncrementId = min,
                        Address = kv.Key,
                        ChainId = pool.ChainId
                    });

                    if (!_priorAddresses.Contains(kv.Key))
                    {
                        // only contract txs could be revert
                        await @lock.WriteLock(() =>
                        {
                            pool.Withdraw(kv.Key, min);
                            foreach (var tx in kv.Value)
                            {
                                if (@lock == PriorTxLock)
                                {
                                    AddPriorTransaction(tx);
                                }
                                else
                                {
                                    AddContractTransaction(tx);
                                }
                            }
                        });
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
            
            _logger?.Log(LogLevel.Debug, "Reverted {0} txs.", txsOut.Count);
        }

        public void SetBlockVolume(int minimal, int maximal)
        {
            _contractTxPool.SetBlockVolume(minimal, maximal);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// A lock for managing asynchronous access to memory pool.
    /// </summary>
    public class TxPoolSchedulerLock : ReaderWriterLock
    {
    }
}