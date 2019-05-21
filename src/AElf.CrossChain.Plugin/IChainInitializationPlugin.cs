using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;
using Google.Protobuf;

namespace AElf.CrossChain.Plugin
{
    public interface IChainInitializationPlugin
    {
        Task<ByteString> RequestChainInitializationContextAsync(int chainId);
    }
}