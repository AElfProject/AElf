using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.ChainController
{
    public interface IChainContextService
    {
        Task<IChainContext> GetChainContextAsync(Hash chainId);
    }
}