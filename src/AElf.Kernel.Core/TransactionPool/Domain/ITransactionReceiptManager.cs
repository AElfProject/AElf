using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Domain
{
    public interface ITransactionReceiptManager
    {
        Task AddOrUpdateReceiptAsync(TransactionReceipt receipt);
        Task AddOrUpdateReceiptsAsync(IEnumerable<TransactionReceipt> receipts);
        Task<TransactionReceipt> GetReceiptAsync(Hash txId);
    }
}