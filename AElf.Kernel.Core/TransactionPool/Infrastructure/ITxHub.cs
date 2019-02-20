using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public interface ITxHub
    {
        Task<bool> AddTransactionAsync(int chainId, Transaction transaction, bool skipValidation=false);

        Task<List<TransactionReceipt>> GetReceiptsOfExecutablesAsync();
        Task<TransactionReceipt> GetCheckedReceiptsAsync(int chainId, Transaction txn);
        Task<TransactionReceipt> GetReceiptAsync(Hash txId);

        bool TryGetTx(Hash txId, out Transaction tx);

        Task OnNewBlock(Block block);
    }
}