using System.Threading.Tasks;
using Acs7;

namespace AElf.CrossChain.Communication.Application
{
    public interface ICrossChainRequestService
    {
        Task RequestCrossChainDataFromOtherChainsAsync();

        Task<ChainInitializationData> RequestChainInitializationDataAsync(int chainId);
    }
}