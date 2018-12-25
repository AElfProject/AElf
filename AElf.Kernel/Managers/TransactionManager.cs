using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;
using NLog;

namespace AElf.Kernel.Managers
{
    public class TransactionManager: ITransactionManager
    {
        private readonly ITransactionStore _transactionStore;
        private readonly ILogger _logger;

        public TransactionManager(ITransactionStore transactionStore)
        {
            _transactionStore = transactionStore;
            _logger = LogManager.GetLogger(nameof(TransactionManager));
        }

        public async Task<Hash> AddTransactionAsync(Transaction tx)
        {
            var txHash = tx.GetHash();
            await _transactionStore.SetAsync(GetStringKey(txHash), tx);
            return txHash;
        }

        public async Task<Transaction> GetTransaction(Hash txId)
        {
            return await _transactionStore.GetAsync<Transaction>(GetStringKey(txId));
        }

        public async Task RemoveTransaction(Hash txId)
        {
            await _transactionStore.RemoveAsync(GetStringKey(txId));
        }
        
        private string GetStringKey(Hash txId)
        {
            return txId.ToHex();
        }
    }
}