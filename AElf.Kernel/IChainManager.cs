using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IChainManager
    {
        Task AddBlockAsync(Chain chain, Block block);
        Task<Chain> GetChainAsync(Hash id);
    }
}