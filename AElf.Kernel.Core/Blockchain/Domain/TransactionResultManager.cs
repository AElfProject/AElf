using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain
{
    public class TransactionResultManager : ITransactionResultManager
    {
        private readonly IBlockchainStore<TransactionResult> _transactionResultStore;

        public TransactionResultManager(IBlockchainStore<TransactionResult> transactionResultStore)
        {
            _transactionResultStore = transactionResultStore;
        }

        public async Task AddTransactionResultAsync(TransactionResult tr)
        {
            await _transactionResultStore.SetAsync(tr.TransactionId.ToHex(), tr);
        }

        public async Task<TransactionResult> GetTransactionResultAsync(Hash txId)
        {
            return await _transactionResultStore.GetAsync(txId.ToHex());
        }
    }
}