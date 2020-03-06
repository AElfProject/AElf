using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;

namespace AElf.CrossChain.Communication.Application
{
    public interface ICrossChainClientService
    {
        Task<ICrossChainClient> GetCrossChainClientAsync(int chainId);
        Task<ICrossChainClient> CreateClientAsync(CrossChainClientCreationContext crossChainClientCreationContext);
        
        Task<ICrossChainClient> CreateChainInitializationClientAsync(int chainId);

        Task CloseClientsAsync();
    }
}