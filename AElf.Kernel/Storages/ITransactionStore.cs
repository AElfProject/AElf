using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ITransactionStore
    {
        Task<IHash> InsertAsync(ITransaction tx);
        Task<ITransaction> GetAsync(Hash hash);
    }
}