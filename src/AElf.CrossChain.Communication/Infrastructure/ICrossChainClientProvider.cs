using System.Collections.Generic;

namespace AElf.CrossChain.Communication.Infrastructure
{
    public interface ICrossChainClientProvider
    {
        ICrossChainClient CreateAndCacheClient(CrossChainClientDto crossChainClientDto);
        bool TryGetClient(int chainId, out ICrossChainClient client);
        ICrossChainClient CreateCrossChainClient(CrossChainClientDto crossChainClientDto);

        List<ICrossChainClient> GetAllClients();
    }
}