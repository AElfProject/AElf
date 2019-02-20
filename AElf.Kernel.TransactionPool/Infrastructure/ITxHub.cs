using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public interface ITxHub : IChainRelatedComponent
    {
        Task<bool> AddTransactionAsync(int chainId, Transaction transaction, bool skipValidation=false);

        Task<List<TransactionReceipt>> GetReceiptsOfExecutablesAsync();
        Task<TransactionReceipt> GetCheckedReceiptsAsync(int chainId, Transaction txn);
        Task<TransactionReceipt> GetReceiptAsync(Hash txId);

        bool TryGetTx(Hash txId, out Transaction tx);

        Task OnNewBlock(Block block);
    }
}