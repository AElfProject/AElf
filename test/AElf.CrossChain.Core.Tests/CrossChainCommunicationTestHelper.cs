using System.Collections.Generic;
using AElf.CrossChain.Communication.Infrastructure;

namespace AElf.CrossChain
{
    public class CrossChainCommunicationTestHelper
    {
        private readonly Dictionary<int, CrossChainClientCreationContext> _crossChainClientCreationContexts =
            new Dictionary<int, CrossChainClientCreationContext>();
        
        private readonly Dictionary<int, bool> _clientConnected = new Dictionary<int, bool>();
        
        public void AddNewCrossChainClient(CrossChainClientCreationContext crossChainClientCreationContext)
        {
            _crossChainClientCreationContexts[crossChainClientCreationContext.RemoteChainId] =
                crossChainClientCreationContext;
        }

        public bool TryGetCrossChainClientCreationContext(int chainId, 
            out CrossChainClientCreationContext crossChainClientCreationContext) 
        {
            return _crossChainClientCreationContexts.TryGetValue(chainId, out crossChainClientCreationContext);
        }
        
        
        public bool CheckClientConnected(int chainId)
        {
            return _clientConnected[chainId];
        }

        public void SetClientConnected(int chainId, bool isConnected)
        {
            _clientConnected[chainId] = isConnected;
        }
    }
}