using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class TransactionResultStore : ITransactionResultStore
    {
        public Task InsertAsync(TransactionResult result)
        {
            throw new System.NotImplementedException();
        }

        public Task<TransactionResult> GetAsync(Hash hash)
        {
            throw new System.NotImplementedException();
        }
    }
}