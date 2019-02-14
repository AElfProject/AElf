using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Miner.TxMemPool
{
    public interface ITxHub
    {
        Task AddTransactionAsync(int chainId, Transaction transaction, bool skipValidation=false);

        Task<List<TransactionReceipt>> GetReceiptsOfExecutablesAsync();
        Task<TransactionReceipt> GetCheckedReceiptsAsync(int chainId, Transaction txn);
        Task<TransactionReceipt> GetReceiptAsync(Hash txId);

        bool TryGetTx(Hash txId, out Transaction tx);

        void Initialize(int chainId);
        
        /// <summary>
        /// open transaction pool
        /// </summary>
        void Start();

        /// <summary>
        /// close transaction pool
        /// </summary>
        Task Stop();

        Task OnNewBlock(Block block);
    }
}