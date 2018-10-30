using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Miner.TxMemPool
{
    public interface ITxHub
    {
        Task AddTransactionAsync(Transaction transaction, bool skipValidation=false);

        Task<List<TransactionReceipt>> GetReceiptsOfExecutablesAsync();
        Task<List<TransactionReceipt>> GetReceiptsForAsync(IEnumerable<Transaction> transactions);
        Task<TransactionReceipt> GetReceiptAsync(Hash txId);

        bool TryGetTx(Hash txId, out Transaction tx);

        void Initialize();
        
        /// <summary>
        /// open transaction pool
        /// </summary>
        void Start();

        /// <summary>
        /// close transaction pool
        /// </summary>
        Task Stop();
    }
}