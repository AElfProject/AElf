using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public interface IChainContextService
    {
        Task<IChainContext> GetChainContextAsync(Hash chainId);
    }
}