using System.Threading.Tasks;

namespace AElf.CrossChain.Communication.Infrastructure
{
    public interface ICrossChainClientProvider
    {
        void CreateAndCacheClient(CrossChainClientDto crossChainClientDto);
        Task<ICrossChainClient> GetClientAsync(int chainId);
        ICrossChainClient CreateCrossChainClient(CrossChainClientDto crossChainClientDto);
        Task CloseClientsAsync();
    }
}