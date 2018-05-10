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

        public async Task AddTransactionAsync(Transaction tx)
        {
            await _transactionStore.InsertAsync(tx);
        }
    }
}