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

        void SetCrossChainBlockDataEntityHandler(Func<IBlockCacheEntity, bool> crossChainBlockDataEntityHandler);
        Task RequestCrossChainDataAsync(long targetHeight);
        Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId);
        Task ConnectAsync();
        Task CloseAsync();
    }
}