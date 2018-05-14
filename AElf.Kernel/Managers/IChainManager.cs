using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IChainManager
    {
        Task AppendBlockToChainAsync(IChain chain, Block block);
        Task<IChain> GetChainAsync(Hash id);
        Task<IChain> AddChainAsync(Hash chainId);
    }
} 