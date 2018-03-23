using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IChainManager
    {
<<<<<<< HEAD
        Task AppenBlockToChainAsync(Chain chain, Block block);
        Task<Chain> GetChainAsync(Hash id);
=======
        Task AddBlockAsync(IChain chain, IBlock block);
        Task<IChain> GetAsync(IHash<IChain> chainId);
>>>>>>> feature-object-manager-20180319
    }
}