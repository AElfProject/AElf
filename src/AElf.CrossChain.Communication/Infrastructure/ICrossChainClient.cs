using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;
using Google.Protobuf;

namespace AElf.CrossChain.Communication.Infrastructure
{
    public interface ICrossChainClient
    {
        int RemoteChainId { get; }
        string TargetUriString { get; }
        Task RequestCrossChainDataAsync(long targetHeight);
        Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId);
        Task<bool> ConnectAsync();
        Task CloseAsync();
    }
}