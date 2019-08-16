using System.Threading.Tasks;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public sealed class GrpcCrossChainServerNodePluginTests : GrpcCrossChainServerTestBase
    {
        private readonly IGrpcServePlugin _grpcCrossChainServerNodePlugin;
        private readonly IGrpcCrossChainServer _grpcCrossChainServer;

        public GrpcCrossChainServerNodePluginTests()
        {
            _grpcCrossChainServer = GetRequiredService<IGrpcCrossChainServer>();
            _grpcCrossChainServerNodePlugin = GetRequiredService<IGrpcServePlugin>();
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