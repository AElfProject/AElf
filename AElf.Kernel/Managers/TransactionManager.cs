using System.Threading.Tasks;
using AElf.Kernel.Storages;
using Google.Protobuf;

namespace AElf.Kernel.Managers
{
    public class TransactionManager: ITransactionManager
    {
        private IDataStore _dataStore;

        public TransactionManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task<Hash> AddTransactionAsync(Transaction tx)
        {
            await _dataStore.InsertAsync(tx.GetHash(), tx);
            return tx.GetHash();
        }

        public async Task<Transaction> GetTransaction(Hash txId)
        {
            return await _dataStore.GetAsync<Transaction>(txId);
        }
    }
}