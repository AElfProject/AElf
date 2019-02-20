using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Node.Infrastructure
{
    public interface IChainRelatedComponent: IDisposable
    {
        Task<IDisposable> StartAsync(int chainId);
        Task StopAsync();
    }
}