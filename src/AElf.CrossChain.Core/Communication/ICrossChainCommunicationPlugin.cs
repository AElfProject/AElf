using System.Threading.Tasks;

namespace AElf.CrossChain.Communication
{
    public interface ICrossChainCommunicationPlugin
    {
        Task StartAsync(int chainId);
        Task ShutdownAsync();

        int ChainId { get; }
    }
}