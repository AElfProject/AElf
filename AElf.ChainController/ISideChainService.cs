using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.ChainController
{
    public interface ISideChainService
    {
        Task RegisterSideChain(Hash chainId, Hash lockedAddress, ulong lockedToken);
        Task UnRegisterSideChain(Hash chainId);
    }
}