using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public sealed class GrpcCrossChainClientNodePluginTests : GrpcCrossChainClientTestBase
    {
        private readonly GrpcCrossChainClientNodePlugin _grpcCrossChainClientNodePlugin;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private IGrpcCrossChainServer _server;

        public GrpcCrossChainClientNodePluginTests()
        {
            _grpcCrossChainClientNodePlugin = GetRequiredService<GrpcCrossChainClientNodePlugin>();
            _grpcCrossChainConfigOption = GetRequiredService<IOptionsSnapshot<GrpcCrossChainConfigOption>>().Value;
            _crossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
            _server = GetRequiredService<IGrpcCrossChainServer>();
        }

        [Fact]
        public async Task ClientStartTest_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var remoteChainId = ChainOptions.ChainId;
            await _server.StartAsync("localhost", 5000);
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
            var client = _crossChainClientProvider.GetAllClients();
            Assert.True(client[0].RemoteChainId == remoteChainId);
            Dispose();
        }

        [Fact]
        public async Task ClientStartTest_Null_Test()
        {
            var chainId = ChainOptions.ChainId;
            _grpcCrossChainConfigOption.RemoteParentChainServerPort = 0;
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
            Dispose();
        }

        [Fact]
        public async Task StopClientTest_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var remoteChainId = ChainOptions.ChainId;
            await _server.StartAsync("localhost", 5000);
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
            var client = _crossChainClientProvider.GetAllClients();
            await client[0].ConnectAsync();
            Assert.True(client[0].IsConnected);
            await _grpcCrossChainClientNodePlugin.ShutdownAsync();
            Assert.False(client[0].IsConnected);
            Dispose();
        }

        public override void Dispose()
        {
            _server?.Dispose();
        }
    }
}