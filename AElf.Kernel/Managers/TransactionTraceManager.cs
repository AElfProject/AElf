using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class TransactionTraceManager : ITransactionTraceManager
    {
        private readonly IDataStore _dataStore;

        private readonly Hash _typeIdHash = Hash.FromString("__TransactionTrace__");
        
        public TransactionTraceManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        private Hash GetDisambiguatedHash(Hash txId, Hash disambiguationHash)
        {
            var hash = disambiguationHash == null ? txId : Hash.Xor(disambiguationHash, txId);
            return Hash.Xor(hash, _typeIdHash);
        }
        
        public async Task AddTransactionTraceAsync(TransactionTrace tr, Hash disambiguationHash = null)
        {
            var trKey = GetDisambiguatedHash(tr.TransactionId, disambiguationHash);
            await _dataStore.InsertAsync(trKey, tr);
        }

        public async Task<TransactionTrace> GetTransactionTraceAsync(Hash txId, Hash disambiguationHash = null)
        {
            var trKey = GetDisambiguatedHash(txId, disambiguationHash);
            return await _dataStore.GetAsync<TransactionTrace>(trKey);
        }
    }
}