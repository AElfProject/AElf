using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public interface ITransactionStore
    {
        Task<Hash> InsertAsync(ITransaction tx);
        Task<ITransaction> GetAsync(Hash hash);
        Task RemoveAsync(Hash hash);
    }
}