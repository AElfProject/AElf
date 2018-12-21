using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Manager.Managers
{
    public class TransactionResultManager : ITransactionResultManager
    {
        private readonly ITransactionResultStore _transactionResultStore;

        public TransactionResultManager(ITransactionResultStore transactionResultStore)
        {
            _transactionResultStore = transactionResultStore;
        }

        public async Task AddTransactionResultAsync(TransactionResult tr)
        {
            await _transactionResultStore.SetAsync(tr.TransactionId.DumpHex(), tr);
        }

        public async Task<TransactionResult> GetTransactionResultAsync(Hash txId)
        {
            return await _transactionResultStore.GetAsync<TransactionResult>(txId.DumpHex());
        }
    }
}