using System;
using System.Threading.Tasks;
using AElf.Standards.ACS7;

namespace AElf.CrossChain.Communication.Infrastructure
{
    public interface ICrossChainClient
    {
        int RemoteChainId { get; }
        string TargetUriString { get; }
        bool IsConnected { get; }

        Task RequestCrossChainDataAsync(long targetHeight,
            Func<ICrossChainBlockEntity, bool> crossChainBlockDataEntityHandler);

        Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId);
        Task ConnectAsync();
        Task CloseAsync();
    }
}