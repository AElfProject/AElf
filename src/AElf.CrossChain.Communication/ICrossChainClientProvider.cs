using System;
using System.Threading.Tasks;

namespace AElf.CrossChain.Communication
{
    public interface ICrossChainClientProvider
    {
        ICrossChainClient CreateClientForChainInitializationData(int chainId);
        void CreateAndCacheClient(CrossChainClientDto crossChainClientDto);
        Task<ICrossChainClient> GetClientAsync(int chainId);
        Task CloseClientsAsync();
    }

    public class CrossChainClientDto
    {
        public string RemoteServerHost { get; set; }
        public int RemoteServerPort { get; set; }
        public int RemoteChainId { get; set; }
        public int LocalChainId { get; set; }

        public bool IsClientToParentChain { get; set; }
    }
}