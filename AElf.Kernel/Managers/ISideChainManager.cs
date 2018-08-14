using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface ISideChainManager
    {
        Task<SideChain> GetSideChainAsync(Hash chainId);
        Task SetSideChainAsync(SideChain sideChain);
    }
}