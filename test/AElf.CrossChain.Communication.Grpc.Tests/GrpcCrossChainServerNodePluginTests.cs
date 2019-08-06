using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public sealed class GrpcCrossChainServerNodePluginTests : GrpcCrossChainServerTestBase
    {
        private readonly GrpcCrossChainServerNodePlugin _grpcCrossChainServerNodePlugin;
        private ICrossChainClientProvider _grpcCrossChainClientProvider;


        public GrpcCrossChainServerNodePluginTests()
        {
            _grpcCrossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
            _grpcCrossChainServerNodePlugin = GetRequiredService<GrpcCrossChainServerNodePlugin>();
        }

        [Fact]
        public async Task CrossChainServerStart_Test()
        {
            var remoteChainId = ChainHelper.GetChainId(1);
            var localChainId = ChainHelper.GetChainId(2);
            await _grpcCrossChainServerNodePlugin.StartAsync(localChainId);
            CreateAndCacheClient(localChainId, false, 5001, remoteChainId);
            var client = _grpcCrossChainClientProvider.GetAllClients();
            Assert.True(client[0].RemoteChainId == remoteChainId);
            Assert.True(client[0].TargetUriString.Equals("localhost:5001"));
        }


        [Fact]
        public async Task CrossChainServerShutdown_Test()
        {
            var remoteChainId = ChainHelper.GetChainId(1);
            var localChainId = ChainHelper.GetChainId(2);
            await _grpcCrossChainServerNodePlugin.StartAsync(localChainId);
            CreateAndCacheClient(localChainId, false, 5001, remoteChainId);
            var client = _grpcCrossChainClientProvider.GetAllClients();
            Assert.True(client[0].RemoteChainId == remoteChainId);
            Assert.True(client[0].TargetUriString.Equals("localhost:5001"));
            await _grpcCrossChainServerNodePlugin.ShutdownAsync();
        }
    }
}