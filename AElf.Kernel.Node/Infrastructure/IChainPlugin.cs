using System.Threading.Tasks;

namespace AElf.Kernel.Node.Infrastructure
{
    public interface IChainPlugin
    {
        Task StartAsync(int chainId);
        Task ShutdownAsync();
    }
}