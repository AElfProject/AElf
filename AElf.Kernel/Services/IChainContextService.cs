using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Services
{
    public interface IChainContextService
    {
        Task<IChainContext> GetChainContextAsync(Hash chainId);
    }
}