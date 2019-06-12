using System.Threading.Tasks;

namespace AElf.CrossChain.Communication
{
    public interface ICrossChainCommunicationController
    {
        Task StartAsync(int chainId);
        Task StopAsync();
    }
}