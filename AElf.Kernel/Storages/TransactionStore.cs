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

        public TransactionStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task<Hash> InsertAsync(ITransaction tx)
        {
            var key = tx.GetHash().GetKeyString(TypeName.TnTransaction);           
            await _keyValueDatabase.SetAsync(key, tx.Serialize());
            return tx.GetHash();
        }

        public async Task<ITransaction> GetAsync(Hash hash)
        {
            var key = hash.GetKeyString(TypeName.TnTransaction);    
            var txBytes = await _keyValueDatabase.GetAsync(key, typeof(ITransaction));
            return txBytes == null ? null : Transaction.Parser.ParseFrom(txBytes);
        }

        public async Task RemoveAsync(Hash hash)
        {
            var key = hash.GetKeyString(TypeName.TnTransaction);   
            await _keyValueDatabase.RemoveAsync(key);
        }
    }
}