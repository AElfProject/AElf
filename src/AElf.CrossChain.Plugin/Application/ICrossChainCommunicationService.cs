using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChain.Plugin.Infrastructure;
using Google.Protobuf;

namespace AElf.CrossChain.Plugin.Application
{
    public interface ICrossChainCommunicationService
    {
        Task RequestCrossChainDataFromOtherChains(IEnumerable<int> chainIds);
        Task ConnectWithNewChainAsync(ICrossChainClientDto crossChainClientDto);

        Task<ByteString> RequestChainInitializationInformationAsync(int chainId);
    }
}