using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IChainManager
    {
        Task AddBlockAsync(IChain chain, IBlock block);
        Task<IChain> GetAsync(IHash<IChain> chainId);
    }
}