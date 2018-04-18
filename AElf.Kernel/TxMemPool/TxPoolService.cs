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

        private readonly HashSet<Transaction> tmp = new HashSet<Transaction>();
        
        public TxPoolService(ITxPool txPool, TxPoolSchedulerLock @lock, ITransactionManager transactionManager)
        {
            _txPool = txPool;
            Lock = @lock;
            _transactionManager = transactionManager;
        }


        public AutoResetEvent ARE { get; } = new AutoResetEvent(false);

        private TxPoolSchedulerLock Lock { get; }

        /// <inheritdoc/>
        public Task<bool> AddTransaction(Transaction tx)
        {
            return Lock.WriteAsync(() => tmp.Add(tx));
        }
        
        /// <inheritdoc/>
        public Task AddTransactions(List<Transaction> txs)
        {
            return Lock.WriteAsync(() =>
            {
                foreach (var tx in txs)
                {
                    _txPool.AddTx(tx);
                }
                return Task.CompletedTask;
            });
        }

        
        /// <summary>
        /// wait new tx
        /// </summary>
        /// <returns></returns>
        public async Task WaitTx()
        {
            // TODO: need interupt waiting 
            while (true)
            {
                // wait for signal
                ARE.WaitOne();

                List<Transaction> txs;
                lock (tmp)
                {
                    txs = tmp.AsParallel().Where(p => !_txPool.Contains(p.From)).ToList();
                    // clear tmp txs
                    tmp.Clear();
                }
                
                if(txs.Count == 0)
                    continue;
                
                await AddTransactions(txs);
            }
        }

        /// <inheritdoc/>
        public Task Remove(Hash txHash)
        {
            Lock.WriteAsync(() => _txPool.DisgardTx(txHash));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveTxWithWorstFee()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task RemoveTxsExecuted(Block block)
        {
            var txHashes = block.Body.Transactions;
            foreach (var hash in txHashes)
            {
                await Remove(hash);
            }

            // Sets the state of the event to signaled, allowing one or more waiting threads to proceed
            ARE.Set();
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
            if (!await GetTransaction(txHash, out var tx))
            {
                // TODO: tx not found, log error
            }
            else
            {
                await _transactionManager.AddTransactionAsync(tx);
            }
        }
        
        /// <inheritdoc/>
        public Task<List<Transaction>> GetReadyTxs()
        {
            return Lock.ReadAsync(() => _txPool.Ready);
        }

        /// <inheritdoc/>
        public Task<ulong> GetPoolSize()
        {
            return Lock.ReadAsync(() => _txPool.Size);
        }

        /// <inheritdoc/>
        public Task<bool> GetTransaction(Hash txHash, out Transaction tx)
        {
            tx = Lock.ReadAsync(() => _txPool.GetTransaction(txHash)).Result;
            return Task.FromResult(tx != null);
        }

        /// <inheritdoc/>
        public Task Clear()
        {
            return Lock.WriteAsync(()=>
            {
                _txPool.ClearAll();
                return Task.CompletedTask;
            });
        }

        /// <inheritdoc/>
        public Task SavePool()
        {
            throw new System.NotImplementedException();
        }
    }
    
    /// <summary>
    /// A lock for managing asynchronous access to memory pool.
    /// </summary>
    public class TxPoolSchedulerLock : ReaderWriterLock
    {
    }
}