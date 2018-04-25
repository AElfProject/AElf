using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using ReaderWriterLock = AElf.Kernel.Lock.ReaderWriterLock;

namespace AElf.Kernel.TxMemPool
{
    public class TxPoolService : ITxPoolService
    {
        private readonly ITxPool _txPool;
        private readonly ITransactionManager _transactionManager;

        public TxPoolService(ITxPool txPool, ITransactionManager transactionManager)
        {
            _txPool = txPool;
            _transactionManager = transactionManager;
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

        /// <inheritdoc/>
        public Task<bool> AddTxAsync(Transaction tx)
        {
            return Cts.IsCancellationRequested ? Task.FromResult(false) : Lock.WriteLock(() =>
            {
                var res = _txPool.AddTx(tx);
                if (_txPool.GetTmpSize() >= _txPool.EntryThreshold)
                {
                    EnqueueEvent.Set();
                }
                return res;
            });
            
        }
        
        /*/// <summary>
        /// add multi txs to tx pool
        /// </summary>
        /// <returns></returns>
        private Task QueueTxsAsync()
        {
            
        }*/

        
        /// <summary>
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
                await Lock.WriteLock(() =>
                {
                    _txPool.QueueTxs();
                    return Task.CompletedTask;
                });
            }
        }
        
        /// <summary>
        /// main loop for tx pool
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private async Task Demote()
        {
            while (!Cts.IsCancellationRequested)
            {
                DemoteEvent.WaitOne();
                await Lock.WriteLock(() =>
                {
                    _txPool.RemoveExecutedTxs();
                    return Task.CompletedTask;
                });
            }
        }
        

        /// <inheritdoc/>
        public Task RemoveAsync(Hash txHash)
        {
            Lock.WriteLock(() => _txPool.DisgardTx(txHash));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveTxWithWorstFeeAsync()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task RemoveTxsExecutedAsync(Block block)
        {
            var txHashes = block.Body.Transactions;
            foreach (var hash in txHashes)
            {
                await RemoveAsync(hash);
            }

            // Sets the state of the event to signaled, allowing one or more waiting threads to proceed
            EnqueueEvent.Set();
        }

        /// <inheritdoc/>
        public async Task PersistTxs(IEnumerable<Hash> txHashes)
        {
            foreach (var h in txHashes)
            {
                await Persist(h);
            }
        }

        private async Task Persist(Hash txHash)
        {
            if (!await GetTxAsync(txHash, out var tx))
            {
                // TODO: tx not found, log error
            }
            else
            {
                await _transactionManager.AddTransactionAsync(tx);
            }
        }
        
        /// <inheritdoc/>
        public Task<List<Transaction>> GetReadyTxsAsync()
        {
            return Lock.ReadLock(() => _txPool.Ready);
        }

        /// <inheritdoc/>
        public Task PromoteAsync()
        {
            return Lock.WriteLock(() =>
            {
                _txPool.Promote();
                return Task.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public Task<ulong> GetPoolSize()
        {
            return Lock.ReadLock(() => _txPool.Size);
        }

        /// <inheritdoc/>
        public Task<bool> GetTxAsync(Hash txHash, out Transaction tx)
        {
            tx = Lock.ReadLock(() => _txPool.GetTx(txHash)).Result;
            return Task.FromResult(tx != null);
        }

        /// <inheritdoc/>
        public Task ClearAsync()
        {
            return Lock.WriteLock(()=>
            {
                _txPool.ClearAll();
                return Task.CompletedTask;
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
        public Task<ulong> GetTmpSizeAsync()
        {
            return Lock.ReadLock(() => _txPool.GetTmpSize());
        }
        

        /// <inheritdoc/>
        public void Start()
        {
            // TODO: more initialization
            EnqueueEvent = new AutoResetEvent(false);
            DemoteEvent = new AutoResetEvent(false);
            Cts = new CancellationTokenSource();
            
            // waiting enqueue tx
            Task.Factory.StartNew(async () => await Receive());
            // waiting demote txs
            Task.Factory.StartNew(async () => await Demote());
        }

        

        /// <inheritdoc/>
        public void Stop()
        {
            // TODO: release resources
            Cts.Cancel();
            EnqueueEvent.Dispose();
        }
    }
    
    /// <summary>
    /// A lock for managing asynchronous access to memory pool.
    /// </summary>
    public class TxPoolSchedulerLock : ReaderWriterLock
    {
    }
}