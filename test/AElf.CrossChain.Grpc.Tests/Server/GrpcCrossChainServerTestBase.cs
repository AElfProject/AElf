using AElf.CrossChain.Communication.Infrastructure;
using AElf.TestBase;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AElf.CrossChain.Grpc.Server
{
    public class GrpcCrossChainServerTestBase : AElfIntegratedTest<GrpcCrossChainServerTestModule>
    {
        private readonly ICrossChainClientProvider _grpcCrossChainClientProvider;

        public GrpcCrossChainServerTestBase()
        {
            _grpcCrossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
        }

        public void CreateAndCacheClient(int chainId, bool toParenChain, int port, int remoteChainId = 0)
        {
            var fakeCrossChainClient = new CrossChainClientCreationContext
            {
                LocalChainId = chainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = toParenChain,
                RemoteServerHost = "localhost",
                RemoteServerPort = port
            };
            _grpcCrossChainClientProvider.AddOrUpdateClient(fakeCrossChainClient);
        }
    }
}