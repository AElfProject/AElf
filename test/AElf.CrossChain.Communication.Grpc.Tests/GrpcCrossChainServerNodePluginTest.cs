using System.Threading.Tasks;
using AElf.Kernel;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public sealed class GrpcCrossChainServerNodePluginTest : GrpcCrossChainServerTestBase
    {
        private readonly GrpcCrossChainServerNodePlugin _grpcCrossChainServerNodePlugin;
        private readonly GrpcCrossChainClientNodePlugin _grpcCrossChainClientNodePlugin;
        
        public GrpcCrossChainServerNodePluginTest()
        {
            _grpcCrossChainServerNodePlugin = GetRequiredService<GrpcCrossChainServerNodePlugin>();
            _grpcCrossChainClientNodePlugin = GetRequiredService<GrpcCrossChainClientNodePlugin>();
        }

        [Fact]
        public async Task ServerStart_Test()
        {
            var chainId = _chainOptions.ChainId;
            await _grpcCrossChainServerNodePlugin.StartAsync(chainId);
        }
        
        [Fact]
        public async Task ClientStart_Test()
        {
            var chainId = _chainOptions.ChainId;
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
        }
        
        [Fact]
        public async Task ServerShutdown_Test()
        {
            await _grpcCrossChainServerNodePlugin.StopAsync();
        }

        public override void Dispose()
        {
            _grpcCrossChainServerNodePlugin?.StopAsync().Wait();
        }
    }
}