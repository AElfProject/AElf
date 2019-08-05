using AElf.CrossChain.Communication.Infrastructure;
using AElf.Kernel;
using AElf.TestBase;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientTestBase : AElfIntegratedTest<GrpcCrossChainClientTestModule>
    {
        protected ChainOptions _chainOptions;
        private ICrossChainClientProvider _grpcCrossChainClientProvider;

        public GrpcCrossChainClientTestBase()
        {
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
            _grpcCrossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
        }

        public ICrossChainClient CreateAndCacheClient(int chainId, bool toParenChain, int port, int remoteChainId = 0)
        {
            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = chainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = toParenChain,
                RemoteServerHost = "localhost",
                RemoteServerPort = port
            };
            var client = _grpcCrossChainClientProvider.CreateAndCacheClient(fakeCrossChainClient);
            return client;
        }
    }
}