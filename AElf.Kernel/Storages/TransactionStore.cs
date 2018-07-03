using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
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

        public async Task<IHash> InsertAsync(ITransaction tx)
        {
            var key = tx.GetHash();
            await _keyValueDatabase.SetAsync(key.Value.ToByteArray().ToHex(), tx.Serialize());
            return key;
        }

        public async Task<ITransaction> GetAsync(Hash hash)
        {
            var txBytes = await _keyValueDatabase.GetAsync(hash.Value.ToByteArray().ToHex(), typeof(ITransaction));
            return txBytes == null ? null : Transaction.Parser.ParseFrom(txBytes);
        }
    }
}