using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public sealed class GrpcCrossChainClientNodePluginTest : GrpcCrossChainClientTestBase
    {
        private readonly GrpcCrossChainClientNodePlugin _grpcCrossChainClientNodePlugin;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly ICrossChainClientProvider _crossChainClientProvider;
        private IGrpcCrossChainServer _server;

        public GrpcCrossChainClientNodePluginTest()
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
            var remoteChainId = _chainOptions.ChainId;
            await _server.StartAsync("localhost", 5000);
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
            var client = await _crossChainClientProvider.GetClientAsync(remoteChainId);
            Assert.True(client.IsConnected);
            Dispose();
        }

        [Fact]
        public async Task ClientStartTest_Null_Test()
        {
            var chainId = _chainOptions.ChainId;
            _grpcCrossChainConfigOption.RemoteParentChainServerPort = 0;
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
            Dispose();
        }

        [Fact]
        public async Task CreateClientTest_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            
            await _server.StartAsync("localhost", 5000);
            var grpcCrossChainClientDto = new CrossChainClientDto()
            {
                RemoteChainId = _chainOptions.ChainId,
                RemoteServerHost = _grpcCrossChainConfigOption.RemoteParentChainServerHost,
                RemoteServerPort = _grpcCrossChainConfigOption.RemoteParentChainServerPort,
                LocalChainId = chainId
            };
            await _grpcCrossChainClientNodePlugin.CreateClientAsync(grpcCrossChainClientDto);
            var client = await _crossChainClientProvider.GetClientAsync(_chainOptions.ChainId);
            Assert.True(client.IsConnected);
            Dispose();
        }

        [Fact]
        public async Task StopClientTest_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var remoteChainId = _chainOptions.ChainId;
            await _server.StartAsync("localhost", 5000);
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
            var client = await _crossChainClientProvider.GetClientAsync(remoteChainId);
            Assert.True(client.IsConnected);
            await _grpcCrossChainClientNodePlugin.StopAsync();
            Assert.False(client.IsConnected);
            Dispose();
        }

        public override void Dispose()
        {
            _server?.Dispose();
        }
    }
}