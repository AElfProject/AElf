using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface ISideChainManager
    {
        Task AddSideChain(Hash chainId);
        Task<SideChainIdList> GetSideChainIdList();
    }
}