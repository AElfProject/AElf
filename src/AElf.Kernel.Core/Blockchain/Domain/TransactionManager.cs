using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Blockchain.Domain
{
    public class TransactionManager: ITransactionManager
    {
        private readonly IBlockchainStore<Transaction> _transactionStore;
        public ILogger<TransactionManager> Logger {get;set;}

        public TransactionManager(IBlockchainStore<Transaction> transactionStore)
        {
            _transactionStore = transactionStore;
            Logger = NullLogger<TransactionManager>.Instance;
        }

        public async Task<Hash> AddTransactionAsync(Transaction tx)
        {
            var transactionId = tx.GetHash();
            await _transactionStore.SetAsync(GetStringKey(transactionId), tx);
            return transactionId;
        }

        public async Task<Transaction> GetTransaction(Hash txId)
        {
            return await _transactionStore.GetAsync(GetStringKey(txId));
        }

        public async Task RemoveTransaction(Hash txId)
        {
            await _transactionStore.RemoveAsync(GetStringKey(txId));
        }
        
        private string GetStringKey(Hash txId)
        {
            return txId.ToStorageKey();
        }
    }
}