using System.Threading.Tasks;
using Acs7;

namespace AElf.CrossChain.Communication.Application
{
    public interface ICrossChainClientService
    {
        Task<ChainInitializationData> RequestChainInitializationData(int chainId);
        
        Task RequestCrossChainDataAsync(int chainId, long targetHeight);

        Task CreateClientAsync(CrossChainClientDto crossChainClientDto);
        Task CloseClientsAsync();
    }
}