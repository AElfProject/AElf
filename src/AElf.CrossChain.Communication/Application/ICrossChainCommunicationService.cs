using System.Threading.Tasks;

namespace AElf.CrossChain.Communication.Application
{
    public interface ICrossChainCommunicationService
    {
        Task InitializeCrossChainCommunicationAsync();
        Task TerminateCrossChainCommunicationAsync();
        void CreateChainCommunication(CrossChainClientDto crossChainClientDto);
    }
}