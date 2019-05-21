using System;
using System.Threading.Tasks;
using AElf.CrossChain.Plugin.Infrastructure;

namespace AElf.CrossChain.Grpc
{
    public interface IGrpcCrossChainClientProvider
    {
        CrossChainGrpcClient CreateClientForChainInitializationInformation(int chainId);
        Task CreateAndCacheClientAsync(ICrossChainClientDto crossChainClientDto);
        Task<CrossChainGrpcClient> GetClientAsync(int chainId);
        Task<T> RequestAsync<T>(CrossChainGrpcClient client, Func<CrossChainGrpcClient, Task<T>> requestFunc);
        Task RequestAsync(CrossChainGrpcClient client, Func<CrossChainGrpcClient, Task> requestFunc);
    }
}