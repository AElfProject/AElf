using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Node
{
    public interface INodeComponent: IDisposable
    {
        Task<IDisposable> StartAsync(int chainId);
        Task StopAsync();
    }
}