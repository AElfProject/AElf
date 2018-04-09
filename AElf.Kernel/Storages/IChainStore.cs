using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChainStore
    {
        Task<Chain> GetAsync(Hash id);
        Task<Chain> UpdateAsync(Chain chain);
        Task<Chain> InsertAsync(Chain chain);
    }
}