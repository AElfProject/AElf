using System.Threading.Tasks;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public sealed class GrpcCrossChainServerNodePluginTest : GrpcCrossChainServerTestBase
    {
        private readonly GrpcCrossChainServerNodePlugin _grpcCrossChainServerNodePlugin;
        private GrpcCrossChainClientProvider _grpcCrossChainClientProvider;


        public GrpcCrossChainServerNodePluginTest()
        {
            _grpcCrossChainClientProvider = GetRequiredService<GrpcCrossChainClientProvider>();
            _grpcCrossChainServerNodePlugin = GetRequiredService<GrpcCrossChainServerNodePlugin>();
        }

        [Fact]
        public async Task ServerStart_Test()
        {
            var remoteChainId = ChainHelper.GetChainId(1);
            var localChainId = ChainHelper.GetChainId(2);
            await _grpcCrossChainServerNodePlugin.StartAsync(localChainId);
            CreateAndCacheClient(localChainId, false, 5001, remoteChainId);
            var client = await _grpcCrossChainClientProvider.GetClientAsync(remoteChainId);
            Assert.True(client.RemoteChainId == remoteChainId);
            Assert.True(client.TargetUriString.Equals("localhost:5001"));
            Assert.True(client.IsConnected);
        }


        [Fact]
        public async Task ServerShutdown_Test()
        {
            var remoteChainId = ChainHelper.GetChainId(1);
            var localChainId = ChainHelper.GetChainId(2);
            await _grpcCrossChainServerNodePlugin.StartAsync(localChainId);
            CreateAndCacheClient(localChainId, false, 5001, remoteChainId);
            var client = await _grpcCrossChainClientProvider.GetClientAsync(remoteChainId);
            Assert.True(client.IsConnected);
            await _grpcCrossChainServerNodePlugin.StopAsync();
        }
    }
}