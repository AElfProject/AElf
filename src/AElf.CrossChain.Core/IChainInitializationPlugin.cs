using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel.Node.Infrastructure;
using Google.Protobuf;

namespace AElf.CrossChain
{
    public interface IChainInitializationPlugin : INodePlugin
    {
        Task<IMessage> RequestChainInitializationContextAsync(int chainId);
    }
}