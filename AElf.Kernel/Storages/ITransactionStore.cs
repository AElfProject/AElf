using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ITransactionStore
    {
        Task InsertAsync(ITransaction tx);
        Task<ITransaction> GetAsync(IHash hash);
    }
    
    public class TransactionStore: ITransactionStore
    {
        public Task InsertAsync(ITransaction tx)
        {
            //return Task.FromResult(0);
            throw new System.NotImplementedException();
        }

        public Task<ITransaction> GetAsync(IHash hash)
        {
            throw new System.NotImplementedException();
        }
    }
}