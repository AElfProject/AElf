using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using ReaderWriterLock = AElf.Kernel.Lock.ReaderWriterLock;

namespace AElf.Kernel.TxMemPool
{
    public class TxPoolService : ITxPoolService
    {
        private readonly ITxPool _txPool;
        private readonly IAccountContextService _accountContextService;

        public TxPoolService(ITxPool txPool, IAccountContextService accountContextService)
        {
            _txPool = txPool;
            _accountContextService = accountContextService;
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
        
        //private HashSet<ITransaction> Tmp { get; } = new HashSet<ITransaction>();
        
        private readonly ConcurrentDictionary<Hash, ulong> _nonces = new ConcurrentDictionary<Hash, ulong>();
        
        private readonly ConcurrentDictionary<Hash, ITransaction> _txs = new ConcurrentDictionary<Hash, ITransaction>();

        /// <inheritdoc/>
        public async Task<bool> AddTxAsync(ITransaction tx)
        {
            // add tx
            _txs.GetOrAdd(tx.GetHash(), tx);
            
            // tx from account state
            if (!_txPool.Nonces.TryGetValue(tx.From, out var id))
            {
                id = (await _accountContextService.GetAccountDataContext(tx.From, _txPool.ChainId)).IncrementId;
                _txPool.Nonces.TryAdd(tx.From, id);
            }
           
            return await (Cts.IsCancellationRequested ? Task.FromResult(false) : Lock.WriteLock(() =>
            {
                return _txPool.EnQueueTx(tx);
            }));
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
        
        /// <summary>
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
        }
        

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
            return Lock.ReadLock(() =>
            {
                _txPool.Enqueueable = false;
                return _txPool.ReadyTxs(limit);
            });
        }

        /// <inheritdoc/>
        public Task<bool> PromoteAsync()
        {
            return Lock.WriteLock(() =>
            {
                _txPool.Promote();
                return true;
            });
        }

        /// <inheritdoc/>
        public Task<ulong> GetPoolSize()
        {
            return Lock.ReadLock(() => _txPool.Size);
        }

        /// <inheritdoc/>
        public ITransaction GetTx(Hash txHash)
        {
            if (_txs.TryGetValue(txHash, out var tx) )
            {
                return tx;
            }
            return null;
        }

        /// <inheritdoc/>
        public Task ClearAsync()
        {
            return Lock.WriteLock(()=>
            {
                _txPool.ClearAll();
            });
        }

        /// <inheritdoc/>
        public Task SavePoolAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<ulong> GetWaitingSizeAsync()
        {
            return Lock.ReadLock(() => _txPool.GetWaitingSize());
        }

        /// <inheritdoc/>
        public Task<ulong> GetExecutableSizeAsync()
        {
            return Lock.ReadLock(() => _txPool.GetExecutableSize());
        }
        
        /// <inheritdoc/>
        /*public Task<ulong> GetTmpSizeAsync()
        {
            return Lock.ReadLock(() => (ulong)Tmp.Count);
        }*/

        /// <inheritdoc/>
        public async Task ResetAndUpdate(List<TransactionResult> txResultList)
        {
            foreach (var res in txResultList)
            {
                var hash = GetTx(res.TransactionId).From;
                _txPool.Nonces.TryGetValue(hash, out var id);
                
                // update account context
                await _accountContextService.SetAccountContext(new AccountDataContext
                {
                    IncrementId = id,
                    Address = hash,
                    ChainId = _txPool.ChainId
                });
            }
        }
        
        /// <inheritdoc/>
        public void Start()
        {
            // TODO: more initialization
            EnqueueEvent = new AutoResetEvent(false);
            DemoteEvent = new AutoResetEvent(false);
            Cts = new CancellationTokenSource();
            
            // waiting enqueue tx
            //var task1 = Task.Factory.StartNew(async () => await Receive());
            
            // waiting demote txs
            var task2 = Task.Factory.StartNew(async () => await Demote());
        }

        
        /// <inheritdoc/>
        public async Task Stop()
        {
            await Lock.WriteLock(() =>
            {
                // TODO: release resources
                Cts.Cancel();
                Cts.Dispose();
                EnqueueEvent.Dispose();
                DemoteEvent.Dispose();
            });
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