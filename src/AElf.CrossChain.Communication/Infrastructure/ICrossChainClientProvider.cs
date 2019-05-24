using System;
using System.Threading.Tasks;

namespace AElf.CrossChain.Communication.Infrastructure
{
    public interface ICrossChainClientProvider
    {
        ICrossChainClient CreateClientForChainInitializationData(int chainId);
        void CreateAndCacheClient(ICrossChainClientDto crossChainClientDto);
        Task<ICrossChainClient> GetClientAsync(int chainId);
        Task<T> RequestAsync<T>(ICrossChainClient client, Func<ICrossChainClient, Task<T>> requestFunc);
        Task RequestAsync(ICrossChainClient client, Func<ICrossChainClient, Task> requestFunc);

        Task CloseClientsAsync();
    }

    public interface ICrossChainClientDto
    {
        string RemoteServerHost { get; set; }
        int RemoteServerPort { get; set; }
        int RemoteChainId { get; set; }
        int LocalChainId { get; set; }
        
        //int ConnectionTimeout { get; set; }

        bool IsClientToParentChain { get; set; }
    }
}