using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class TransactionTraceManager : ITransactionTraceManager
    {
        private readonly ITransactionTraceStore _transactionTraceStore;
        
        public TransactionTraceManager(ITransactionTraceStore transactionTraceStore)
        {
            _transactionTraceStore = transactionTraceStore;
        }

        private string GetDisambiguatedKey(Hash txId, Hash disambiguationHash)
        {
            var hash = disambiguationHash == null ? txId : Hash.Xor(disambiguationHash, txId);
            return hash.ToHex();
        }
        
        public async Task AddTransactionTraceAsync(TransactionTrace tr, Hash disambiguationHash = null)
        {
            var key = GetDisambiguatedKey(tr.TransactionId, disambiguationHash);
            await _transactionTraceStore.SetAsync(key, tr);
        }

        public async Task<TransactionTrace> GetTransactionTraceAsync(Hash txId, Hash disambiguationHash = null)
        {
            var key = GetDisambiguatedKey(txId, disambiguationHash);
            return await _transactionTraceStore.GetAsync<TransactionTrace>(key);
        }
    }
}