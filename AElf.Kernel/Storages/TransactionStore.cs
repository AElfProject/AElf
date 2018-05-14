using System.Linq;
using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;

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
            var key = tx.GetHash();
            await _keyValueDatabase.SetAsync(key.Value.ToBase64(), tx.Serialize());
            return key;
        }

        public async Task<ITransaction> GetAsync(Hash hash)
        {
            var txBytes = await _keyValueDatabase.GetAsync(hash.Value.ToBase64(), typeof(ITransaction));
            return Transaction.Parser.ParseFrom(txBytes);
        }
    }
}