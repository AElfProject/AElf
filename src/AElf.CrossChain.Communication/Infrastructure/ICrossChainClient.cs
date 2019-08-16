using System;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache;

namespace AElf.CrossChain.Communication.Infrastructure
{
    public interface ICrossChainClient
    {
        int RemoteChainId { get; }
        string TargetUriString { get; }
        bool IsConnected { get; }
        Task RequestCrossChainDataAsync(long targetHeight, Func<IBlockCacheEntity, bool> crossChainBlockDataEntityHandler);
        Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId);
        Task ConnectAsync();
        Task CloseAsync();
    }
}