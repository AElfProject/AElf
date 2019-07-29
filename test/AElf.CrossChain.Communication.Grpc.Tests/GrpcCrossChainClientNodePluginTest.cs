using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public sealed class GrpcCrossChainClientNodePluginTest : GrpcCrossChainClientTestBase
    {
        private readonly GrpcCrossChainServerNodePlugin _grpcCrossChainServerNodePlugin;
        private readonly GrpcCrossChainClientNodePlugin _grpcCrossChainClientNodePlugin;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;

        public GrpcCrossChainClientNodePluginTest()
        {
            _grpcCrossChainServerNodePlugin = GetRequiredService<GrpcCrossChainServerNodePlugin>();
            _grpcCrossChainClientNodePlugin = GetRequiredService<GrpcCrossChainClientNodePlugin>();
            _grpcCrossChainConfigOption = GetRequiredService<IOptionsSnapshot<GrpcCrossChainConfigOption>>().Value;
        }

        [Fact]
        public async Task ServerStartTest_Test()
        {
            var chainId = _chainOptions.ChainId;
            await _grpcCrossChainServerNodePlugin.StartAsync(chainId);
        }

        [Fact]
        public async Task ClientStartTest_Test()
        {
            var chainId = _chainOptions.ChainId;
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
        }

        [Fact]
        public async Task ClientStartTest_Null_Test()
        {
            var chainId = _chainOptions.ChainId;
            _grpcCrossChainConfigOption.RemoteParentChainServerPort = 0;
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
        }

        [Fact]
        public async Task CreateClientTest_Test()
        {
            var grpcCrossChainClientDto = new CrossChainClientDto()
            {
                RemoteChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
                RemoteServerHost = _grpcCrossChainConfigOption.RemoteParentChainServerHost,
                RemoteServerPort = _grpcCrossChainConfigOption.RemoteParentChainServerPort
            };
            await _grpcCrossChainClientNodePlugin.CreateClientAsync(grpcCrossChainClientDto);
        }
        
        [Fact]
        public async Task StopClientTest_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
            await _grpcCrossChainClientNodePlugin.StopAsync();
        }

        public override void Dispose()
        {
            _grpcCrossChainServerNodePlugin?.StopAsync().Wait();
        }
    }
}