using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ITransactionStore
    {
        Task InsertAsync(ITransaction tx);
        Task<ITransaction> GetAsync(IHash<ITransaction> hash);
    }
    
    public class TransactionStore: ITransactionStore
    {
        public Task InsertAsync(ITransaction tx)
        {
            throw new System.NotImplementedException();
        }

        public Task<ITransaction> GetAsync(IHash<ITransaction> hash)
        {
            throw new System.NotImplementedException();
        }
    }
}