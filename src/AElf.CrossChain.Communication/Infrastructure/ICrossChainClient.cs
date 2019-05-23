using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using Google.Protobuf;

namespace AElf.CrossChain.Communication.Infrastructure
{
    public interface ICrossChainClient
    {
        int ChainId { get; }
        string TargetUriString { get; }
        Task RequestCrossChainDataAsync(long targetHeight);
        Task<ChainInitializationData> RequestChainInitializationContext(int chainId);
        Task<bool> ConnectAsync();
        Task CloseAsync();
    }
}