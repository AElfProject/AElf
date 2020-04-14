using System.Threading.Tasks;
using AElf.CrossChain.Communication;
using Xunit;

namespace AElf.CrossChain.Grpc.Server
{
    public sealed class GrpcCrossChainServerNodePluginTests : GrpcCrossChainServerTestBase
    {
        private readonly ICrossChainCommunicationPlugin _grpcCrossChainServerNodePlugin;
        private readonly IGrpcCrossChainServer _grpcCrossChainServer;

        public GrpcCrossChainServerNodePluginTests()
        {
            _grpcCrossChainServer = GetRequiredService<IGrpcCrossChainServer>();
            _grpcCrossChainServerNodePlugin = GetRequiredService<ICrossChainCommunicationPlugin>();
        }

        [Fact]
        public async Task CrossChainServerStart_Test()
        {
            var localChainId = ChainHelper.GetChainId(1);
            await _grpcCrossChainServerNodePlugin.StartAsync(localChainId);
            Assert.True(_grpcCrossChainServer.IsStarted);
            await _grpcCrossChainServerNodePlugin.ShutdownAsync();
            Assert.False(_grpcCrossChainServer.IsStarted);
        }
    }
}