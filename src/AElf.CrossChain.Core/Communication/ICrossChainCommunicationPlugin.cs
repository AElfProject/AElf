using System.Threading.Tasks;

namespace AElf.CrossChain.Communication;

public interface ICrossChainCommunicationPlugin
{
    int ChainId { get; }
    Task StartAsync(int chainId);
    Task ShutdownAsync();
}