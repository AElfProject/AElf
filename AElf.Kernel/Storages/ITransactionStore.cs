using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ITransactionStore
    {
        Task InsertAsync(ITransaction tx);
        Task<ITransaction> GetAsync(IHash<ITransaction> hash);
    }
}