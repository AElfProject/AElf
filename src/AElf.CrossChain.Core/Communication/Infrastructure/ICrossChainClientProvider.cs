using System.Collections.Generic;

namespace AElf.CrossChain.Communication.Infrastructure
{
    public interface ICrossChainClientProvider
    {
        ICrossChainClient AddOrUpdateClient(CrossChainClientCreationContext crossChainClientCreationContext);
        bool TryGetClient(int chainId, out ICrossChainClient client);
        ICrossChainClient CreateChainInitializationDataClient(CrossChainClientCreationContext crossChainClientCreationContext);

        List<ICrossChainClient> GetAllClients();
    }
}