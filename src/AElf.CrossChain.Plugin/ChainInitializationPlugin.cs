using System.Threading.Tasks;
using AElf.CrossChain.Plugin.Application;
using AElf.CrossChain.Plugin.Infrastructure;
using Google.Protobuf;

namespace AElf.CrossChain.Plugin
{
    public class ChainInitializationPlugin : IChainInitializationPlugin
    {
        private readonly ICrossChainCommunicationService _crossChainCommunicationService;

        public ChainInitializationPlugin(ICrossChainCommunicationService crossChainCommunicationService)
        {
            _crossChainCommunicationService = crossChainCommunicationService;
        }

        public async Task<ByteString> RequestChainInitializationContextAsync(int chainId)
        {
            var chainInitializationInformation =
                await _crossChainCommunicationService.RequestChainInitializationInformationAsync(chainId);
            return chainInitializationInformation;
        }
    }
}