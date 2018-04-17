using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Lock;
using AElf.Kernel.Storages;

namespace AElf.Kernel.TxMemPool
{
    public class TxPoolService : ITxPoolService
    {
        private readonly ITxPool _txPool;
        private ITransactionManager _transactionManager;

        public TxPoolService(ITxPool txPool, TxPoolSchedulerLock @lock, ITransactionManager transactionManager)
        {
            _txPool = txPool;
            Lock = @lock;
            _transactionManager = transactionManager;
        }
        
        private TxPoolSchedulerLock Lock { get; }

        public Task<bool> AddTransaction(ITransaction tx)
        {
            return Lock.WriteAsync(() => _txPool.AddTx(tx));
        }
        
        public Task AddTransactions(List<ITransaction> txs)
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

        public Task Remove(Hash txHash)
        {
            Lock.WriteAsync(() => _txPool.DisgardTx(txHash));
            return Task.CompletedTask;
        }

        public Task RemoveTxWithWorstFee()
        {
            throw new System.NotImplementedException();
        }

        public async Task RemoveTxsExecuted(Block block)
        {
            var txHashes = block.Body.Transactions;
            foreach (var hash in txHashes)
            {
                await Remove(hash);
            }       
        }

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
                await _transactionManager.AddTransactionAsync((Transaction)tx);
            }
        }
        
        public Task<List<ITransaction>> GetReadyTxs()
        {
            return Lock.ReadAsync(() => _txPool.Ready);
        }

        public Task<ulong> GetPoolSize()
        {
            return Lock.ReadAsync(() => _txPool.Size);
        }

        public Task<bool> GetTransaction(Hash txHash, out ITransaction tx)
        {
            tx = Lock.ReadAsync(() => _txPool.GetTransaction(txHash)).Result;
            return Task.FromResult(tx != null);
        }

        public Task Clear()
        {
            throw new System.NotImplementedException();
        }

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