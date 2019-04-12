using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;

namespace AElf.CrossChain.Grpc
{
    public interface IChainInitializationPlugin : INodePlugin
    {
        Task<ChainInitializationContext> RequestChainInitializationContextAsync(int chainId);
    }
}