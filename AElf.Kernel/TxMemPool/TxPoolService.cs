using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Types;
using ReaderWriterLock = AElf.Common.Synchronisation.ReaderWriterLock;

namespace AElf.Kernel.TxMemPool
{
    public class TxPoolService : ITxPoolService
    {
        private readonly ITxPool _txPool;
        private readonly IAccountContextService _accountContextService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;

        public TxPoolService(ITxPool txPool, IAccountContextService accountContextService, 
            ITransactionManager transactionManager, ITransactionResultManager transactionResultManager)
        {
            _txPool = txPool;
            _accountContextService = accountContextService;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
        }

        /// <summary>
        /// signal event for enqueue txs
        /// </summary>
        private AutoResetEvent EnqueueEvent { get; set; }
        
        /// <summary>
        /// signal event for demote executed txs
        /// </summary>
        private AutoResetEvent DemoteEvent { get; set; }
        
        /// <summary>
        /// Signals to a CancellationToken that it should be canceled
        /// </summary>
        private CancellationTokenSource Cts { get; set; } 
        
        private TxPoolSchedulerLock Lock { get; } = new TxPoolSchedulerLock();
        
        private readonly ConcurrentDictionary<Hash, ITransaction> _txs = new ConcurrentDictionary<Hash, ITransaction>();

        private readonly HashSet<Hash> _addrCache = new HashSet<Hash>();

        /// <inheritdoc/>
        public async Task<TxValidation.TxInsertionAndBroadcastingError> AddTxAsync(ITransaction tx)
        {
            if (!_addrCache.Contains(tx.From))
            {
                // tx from account state
                var incrementId = (await _accountContextService.GetAccountDataContext(tx.From, _txPool.ChainId)).IncrementId;
                _txPool.TryAddNonce(tx.From, incrementId);
                _addrCache.Add(tx.From);
            }
            
            if(!Cts.IsCancellationRequested)
            {
                lock (this)
                {
                    var res = _txPool.EnQueueTx(tx);
                    if (res == TxValidation.TxInsertionAndBroadcastingError.Success)
                    {
                        // add tx
                        _txs.GetOrAdd(tx.GetHash(), tx);
                    }

                    return res;
                }
                
            }
            return TxValidation.TxInsertionAndBroadcastingError.PoolClosed;
            /*return await (Cts.IsCancellationRequested ? Task.FromResult(false) : Lock.WriteLock(() =>
            {
                return _txPool.EnQueueTx(tx);
            }));*/
        }
       
        
        /*/// <summary>
        /// wait new tx
        /// </summary> 
        /// <returns></returns>
        private async Task Receive()
        {
            // TODO: need interupt waiting 
            while (!Cts.IsCancellationRequested)
            {
                // wait for signal
                EnqueueEvent.WaitOne();
                
                if(!_txPool.Enqueueable)
                    continue;
                
                await Lock.WriteLock(() =>
                {
                    foreach (var t in Tmp)
                    {
                        if(_txPool.Nonces.ContainsKey(t.From))
                            continue;
                        if(_nonces.TryGetValue(t.From, out var idValue))
                            _txPool.Nonces[t.From] = idValue;
                    }
                    
                    _txPool.QueueTxs(Tmp); 
                    Tmp.Clear();
                });
            }
        }*/
        
        /*/// <summary>
        /// wait signal to demote txs
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task Demote()
        {
            while (!Cts.IsCancellationRequested)
            {
                DemoteEvent.WaitOne();
            }
        }*/
        

        /// <inheritdoc/>
        public Task RemoveAsync(Hash txHash)
        {
            return !_txs.TryGetValue(txHash, out var tx) ? Task.CompletedTask : Lock.WriteLock(() => _txPool.DiscardTx(tx));
        }

        /// <inheritdoc/>
        public Task RemoveTxWithWorstFeeAsync()
        {
            throw new NotImplementedException();
        }

        
        /// <summary>
        /// persist txs with storage
        /// </summary>
        /// <param name="txs"></param>
        /// <returns></returns>
        private void PersistTxs(IEnumerable<ITransaction> txs)
        {
            foreach (var t in txs)
            {
                // TODO: persist tx
                //await _transactionManager.AddTransactionAsync(tx);
            }
        }

        /// <inheritdoc/>
        public Task<List<ITransaction>> GetReadyTxsAsync(ulong limit)
        {
            //return Lock.ReadLock(() =>
            lock (this)
            {
                //_txPool.Enqueueable = false;
                return Task.FromResult(_txPool.ReadyTxs(limit));
            }
        }

        /// <inheritdoc/>
        public Task<List<ITransaction>> GetReadyTxsAsync(Hash addr, ulong start, ulong ids)
        {
            lock (this)
            {
                return Task.FromResult(_txPool.ReadyTxs(addr, start, ids));
            }
        }
        
        /// <inheritdoc/>
        public Task<bool> PromoteAsync()
        {
            //return Lock.WriteLock(() =>
            lock (this)
            {
                _txPool.Promote();
                return Task.FromResult(true);
            }
        }

        
        /// <inheritdoc/>
        public Task PromoteAsync(List<Hash> addresses)
        {
            //return Lock.WriteLock(() =>
            lock (this)
            {
                _txPool.Promote(addresses);
                return Task.FromResult(true);
            }
        }

        /// <inheritdoc/>
        public Task<ulong> GetPoolSize()
        {
            lock (this)
            {
                return Task.FromResult(_txPool.Size);
            }
            //return Lock.ReadLock(() => _txPool.Size);
        }

        /// <inheritdoc/>
        public bool TryGetTx(Hash txHash, out ITransaction tx)
        {
            return _txs.TryGetValue(txHash, out tx);
        }

        /// <inheritdoc/>
        public Task ClearAsync()
        {
            /*return Lock.WriteLock(()=>
            {
                _txPool.ClearAll();
            });*/
            lock (this)
            {
                _txPool.ClearAll();
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        public Task SavePoolAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ulong> GetWaitingSizeAsync()
        {
            //return Lock.ReadLock(() => _txPool.GetWaitingSize());
            lock (this)
            {
                return Task.FromResult(_txPool.GetWaitingSize());
            }
        }

        /// <inheritdoc/>
        public Task<ulong> GetExecutableSizeAsync()
        {
            //return Lock.ReadLock(() => _txPool.GetExecutableSize());
            lock (this)
            {
                return Task.FromResult(_txPool.GetExecutableSize());
            }
        }
        
        /// <inheritdoc/>
        /*public Task<ulong> GetTmpSizeAsync()
        {
            return Lock.ReadLock(() => (ulong)Tmp.Count);
        }*/

        /// <inheritdoc/>
        public async Task ResetAndUpdate(List<TransactionResult> txResultList)
        {
            var addrs = new HashSet<Hash>();
            foreach (var res in txResultList)
            {
                if (!TryGetTx(res.TransactionId, out var tx))
                    continue;
                addrs.Add(tx.From);
                await _transactionManager.AddTransactionAsync(tx);
                await _transactionResultManager.AddTransactionResultAsync(res);
            }

            foreach (var addr in addrs)
            {
                _txPool.Nonces.TryGetValue(addr, out var id);
                
                // update account context
                await _accountContextService.SetAccountContext(new AccountDataContext
                {
                    IncrementId = id,
                    Address = addr,
                    ChainId = _txPool.ChainId
                });
            }
        }
        
        /// <inheritdoc/>
        public void Start()
        {
            // TODO: more initialization
            //EnqueueEvent = new AutoResetEvent(false);
            //DemoteEvent = new AutoResetEvent(false);
            Cts = new CancellationTokenSource();
            
            // waiting enqueue tx
            //var task1 = Task.Factory.StartNew(async () => await Receive());
            
            // waiting demote txs
            //var task2 = Task.Factory.StartNew(async () => await Demote());
        }

        
        /// <inheritdoc/>
        public Task Stop()
        {
            /*await Lock.WriteLock(() =>
            {
                // TODO: release resources
                Cts.Cancel();
                Cts.Dispose();
                //EnqueueEvent.Dispose();
                //DemoteEvent.Dispose();
            });*/
            lock (this)
            {
                Cts.Cancel();
                Cts.Dispose();
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc/>
        public ulong GetIncrementId(Hash addr)
        {
            lock (this)
            {
                return _txPool.GetPendingIncrementId(addr);
            }
        }
        
        private Dictionary<Hash, List<ITransaction>> GetTxListInPool()
        {
            var res = new Dictionary<Hash, List<ITransaction>>();
            res = _txs.Aggregate(res, 
                (current, p) =>
                {
                    if (!current.TryGetValue(p.Value.From, out var txs))
                    {
                        current[p.Key] = new List<ITransaction>();
                    }
                    res[p.Key].Add(p.Value);
                    return current;
                });
            return res;
        }

        public void RollBack(List<ITransaction> txsOut)
        {
            lock (this)
            {
                var tmap = txsOut.Aggregate(new Dictionary<Hash, HashSet<ITransaction>>(), (current, p) =>
                {
                    if (!current.TryGetValue(p.From, out var txs))
                    {
                        current[p.From] = new HashSet<ITransaction>();
                    }

                    current[p.From].Add(p);
                    return current;
                });

                foreach (var kv in tmap)
                {
                    _txPool.RollBack(kv.Key, (ulong) kv.Value.Count);
                    _txPool.EnQueueTxs(kv.Value);
                }
            }
            
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