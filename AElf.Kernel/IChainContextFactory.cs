using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IChainContextFactory
    {
        Task<IChainContext> GetChainContext(IHash<IChain> chainId);
    }
}