using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class TransactionManager: ITransactionManager
    {
        private readonly ITransactionStore _transactionStore;

        public TransactionManager(ITransactionStore transactionStore)
        {
            _transactionStore = transactionStore;
        }

        public async Task<IHash> AddTransactionAsync(ITransaction tx)
        {
            return await _transactionStore.InsertAsync(tx);
        }

        public async Task<ITransaction> GetTransaction(Hash txId)
        {
            return await _transactionStore.GetAsync(txId);
        }
    }
}