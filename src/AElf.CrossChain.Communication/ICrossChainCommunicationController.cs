using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.CrossChain.Communication
{
    public interface ICrossChainCommunicationController
    {
        Task StartAsync(int chainId);
        Task StopAsync();
    }
}