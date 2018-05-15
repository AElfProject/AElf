using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChainStore
    {
        Task<IChain> GetAsync(Hash id);
        Task<IChain> UpdateAsync(IChain chain);
        Task<IChain> InsertAsync(IChain chain);
    }
}