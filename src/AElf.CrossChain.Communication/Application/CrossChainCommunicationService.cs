using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;

namespace AElf.CrossChain.Communication.Application
{

    public class CrossChainCommunicationService : ICrossChainCommunicationService
    {
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private readonly ICrossChainCommunicationController _crossChainCommunicationController;

        public CrossChainCommunicationService(ICrossChainClientProvider crossChainClientProvider, 
            ICrossChainCommunicationController crossChainCommunicationController)
        {
            _crossChainClientProvider = crossChainClientProvider;
            _crossChainCommunicationController = crossChainCommunicationController;
        }

        public Task InitializeCrossChainCommunicationAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task TerminateCrossChainCommunicationAsync()
        {
            throw new System.NotImplementedException();
        }

        void ICrossChainCommunicationService.CreateChainCommunication(CrossChainClientDto crossChainClientDto)
        {
            throw new System.NotImplementedException();
        }
    }
}