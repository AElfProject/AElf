using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChainStore
    {
        Task Insert(IChain chain);
        Task<IChain> GetAsync(IHash<IChain> chainId);
    }
}