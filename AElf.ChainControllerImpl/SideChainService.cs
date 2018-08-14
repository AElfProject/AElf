using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.ChainController
{
    public class SideChainService : ISideChainService
    {
        public Task RegisterSideChain(Hash chainId, Hash lockedAddress, ulong lockedToken)
        {
            throw new System.NotImplementedException();
        }

        public Task UnRegisterSideChain(Hash chainId)
        {
            throw new System.NotImplementedException();
        }
    }
}