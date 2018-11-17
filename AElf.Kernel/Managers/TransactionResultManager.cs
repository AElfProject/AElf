using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public class TransactionResultManager : ITransactionResultManager
    {
        private readonly IDataStore _dataStore;
        public TransactionResultManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task AddTransactionResultAsync(TransactionResult tr)
        {
            var trKey = DataPath.CalculatePointerForTxResult(tr.TransactionId);
            await _dataStore.InsertAsync(trKey, tr);
        }

        public async Task<TransactionResult> GetTransactionResultAsync(Hash txId)
        {
            var trKey = DataPath.CalculatePointerForTxResult(txId);
            return await _dataStore.GetAsync<TransactionResult>(trKey);
        }
    }
}