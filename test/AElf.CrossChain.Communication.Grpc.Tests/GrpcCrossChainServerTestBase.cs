using AElf.TestBase;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainServerTestBase : AElfIntegratedTest<GrpcCrossChainServerTestModule>
    {
        private GrpcCrossChainClientProvider _grpcCrossChainClientProvider;

        public GrpcCrossChainServerTestBase()
        {
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