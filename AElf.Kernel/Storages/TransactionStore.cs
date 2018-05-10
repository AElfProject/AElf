using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    /// <summary>
    /// Simply use a dictionary to store transactions.
    /// </summary>
    public class TransactionStore : ITransactionStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public TransactionStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task InsertAsync(Transaction tx)
        {
            await _keyValueDatabase.SetAsync(tx.GetHash(), tx);
        }

        public async Task<Transaction> GetAsync(Hash hash)
        {
            return (Transaction) await _keyValueDatabase.GetAsync(hash, typeof(Transaction));
        }
    }
}