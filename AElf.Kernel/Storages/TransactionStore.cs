using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    /// <summary>
    /// Simply use a dictionary to store transactions.
    /// </summary>
    public class TransactionStore : ITransactionStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;
        private static uint TypeIndex => (uint) Types.Transaction;

        public TransactionStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task<Hash> InsertAsync(ITransaction tx)
        {
            var key = tx.GetHash().GetKeyString(TypeIndex);           
            await _keyValueDatabase.SetAsync(key, tx.Serialize());
            return tx.GetHash();
        }

        public async Task<ITransaction> GetAsync(Hash hash)
        {
            var key = hash.GetKeyString(TypeIndex);    
            var txBytes = await _keyValueDatabase.GetAsync(key, typeof(ITransaction));
            return txBytes == null ? null : Transaction.Parser.ParseFrom(txBytes);
        }

        public async Task RemoveAsync(Hash hash)
        {
            var key = hash.GetKeyString(TypeIndex);   
            await _keyValueDatabase.RemoveAsync(key);
        }
    }
}