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

        public async Task<IHash> InsertAsync(ITransaction tx)
        {
            Hash key = tx.GetHash();
            await _keyValueDatabase.SetAsync(key, tx);
            return key;
        }

        public async Task<ITransaction> GetAsync(Hash hash)
        {
            return (Transaction) await _keyValueDatabase.GetAsync(hash, typeof(Transaction));
        }
    }
}