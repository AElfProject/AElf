using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Lock;
using AElf.Kernel.Storages;

namespace AElf.Kernel.TxMemPool
{
    public class TxPoolManager : ITxPoolManager
    {
        private ITxPool _txPool;
        private ITransactionStore _transactionStore;

        public TxPoolManager(ITxPool txPool, ITransactionStore transactionStore, TxPoolSchedulerLock @lock)
        {
            _txPool = txPool;
            _transactionStore = transactionStore;
            Lock = @lock;
        }
        
        public TxPoolSchedulerLock Lock { get; }

        public Task<bool> AddTransaction(ITransaction tx)
        {
            throw new System.NotImplementedException();
        }
        
        public Task<bool> AddTransactions(List<ITransaction> txs)
        {
            throw new System.NotImplementedException();
        }

        public Task Remove(Hash txHash)
        {
            throw new System.NotImplementedException();
        }

        public Task RemoveTxAsWorstFee()
        {
            throw new System.NotImplementedException();
        }

        public Task RemoveTxWithWorstFee()
        {
            throw new System.NotImplementedException();
        }

        public Task RemoveTxsExecuted(ulong blockHeight)
        {
            throw new System.NotImplementedException();
        }

        public Task RemoveTxsInValid()
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> GetPoolSize()
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> GetTransaction(Hash txHash, out ITransaction tx)
        {
            throw new System.NotImplementedException();
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