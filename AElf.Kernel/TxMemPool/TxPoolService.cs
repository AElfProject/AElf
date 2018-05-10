using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReaderWriterLock = AElf.Kernel.Lock.ReaderWriterLock;

namespace AElf.Kernel.TxMemPool
{
    public class TxPoolService : ITxPoolService
    {
        private readonly ITxPool _txPool;

        public TxPoolService(ITxPool txPool)
        {
            _txPool = txPool;
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
                if (_txPool.TmpSize >= _txPool.EntryThreshold)
                {
                    EnqueueEvent.Set();
                }
                return res;
            });
        }
        
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
                });
            }
        }
        
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
                await Lock.WriteLock(() =>
                {
                    var res = _txPool.RemoveExecutedTxs();
                    PersistTxs(res);
                });
            }
        }
        

        /// <inheritdoc/>
        public Task RemoveAsync(Hash txHash)
        {
            return Lock.WriteLock(() => _txPool.DiscardTx(txHash));
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
        private void PersistTxs(IEnumerable<Transaction> txs)
        {
            foreach (var t in txs)
            {
                // TODO: persist tx
                //await _transactionManager.AddTransactionAsync(tx);
            }
        }

        /// <inheritdoc/>
        public Task<List<Transaction>> GetReadyTxsAsync()
        {
            return Lock.ReadLock(() => _txPool.ReadyTxs());
        }

        /// <inheritdoc/>
        public Task PromoteAsync()
        {
            return Lock.WriteLock(() =>
            {
                _txPool.Promote();
            });
        }

        /// <inheritdoc/>
        public Task<ulong> GetPoolSize()
        {
            return Lock.ReadLock(() => _txPool.Size);
        }

        /// <inheritdoc/>
        public Task<Transaction> GetTxAsync(Hash txHash)
        {
            return Lock.ReadLock(() => _txPool.GetTx(txHash));
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
        public Task<ulong> GetTmpSizeAsync()
        {
            return Lock.ReadLock(() => _txPool.TmpSize);
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
        public async Task Stop()
        {
            await Lock.WriteLock(() =>
            {
                // TODO: release resources
                Cts.Cancel();
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