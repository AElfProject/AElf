using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.TxMemPool
{
    public class TxPoolManager : ITxPoolManager
    {
        private ITxPool _txPool;
        private ITransactionStore _transactionStore;

        public TxPoolManager(ITxPool txPool, ITransactionStore transactionStore)
        {
            _txPool = txPool;
            _transactionStore = transactionStore;
        }

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
}