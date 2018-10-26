using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Miner.TxMemPool
{
    public interface ITxHub
    {
        Task AddTransactionAsync(Transaction transaction, bool skipValidation=false);

        Task<List<Transaction>> GetExecutableTransactionsAsync();

        bool TryGetTx(Hash txId, out Transaction tx);
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