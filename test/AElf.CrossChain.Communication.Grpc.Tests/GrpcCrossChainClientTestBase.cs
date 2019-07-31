using AElf.Kernel;
using AElf.TestBase;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientTestBase : AElfIntegratedTest<GrpcCrossChainClientTestModule>
    {
        protected ChainOptions _chainOptions;
        private GrpcCrossChainClientProvider _grpcCrossChainClientProvider;

        public GrpcCrossChainClientTestBase()
        {
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
            _grpcCrossChainClientProvider = GetRequiredService<GrpcCrossChainClientProvider>();
        }

        public void CreateAndCacheClient(int chainId, bool toParenChain, int port, int remoteChainId = 0)
        {
            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = chainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = toParenChain,
                RemoteServerHost = "localhost",
                RemoteServerPort = port
            };
            _grpcCrossChainClientProvider.CreateAndCacheClient(fakeCrossChainClient);
        }
    }
}