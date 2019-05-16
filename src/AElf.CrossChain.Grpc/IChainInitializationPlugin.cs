using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel.Node.Infrastructure;
using Google.Protobuf;

namespace AElf.CrossChain.Grpc
{
    public interface IChainInitializationPlugin : INodePlugin
    {
        Task<SideChainInitializationInformation> RequestChainInitializationContextAsync(int chainId);
    }
}