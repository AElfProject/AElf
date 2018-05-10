using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public class TransactionResultManager : ITransactionResultManager
    {
        //TODO: access to result storage
        public Task AddTransactionResultAsync(TransactionResult tr)
        {
            throw new System.NotImplementedException();
        }

        public Task<TransactionResult> GetTransactionResultAsync(Hash txId)
        {
            throw new System.NotImplementedException();
        }
    }
}