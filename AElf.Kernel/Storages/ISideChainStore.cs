using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface ISideChainStore
    {
        Task<SideChain> GetAsync(Hash chainId);
        Task InsertAsync(SideChain sideChain);
    }
}