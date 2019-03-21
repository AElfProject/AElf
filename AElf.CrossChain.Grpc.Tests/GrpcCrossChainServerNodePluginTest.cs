using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Node.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.CrossChain.Grpc
{
    public sealed class GrpcCrossChainServerNodePluginTest : GrpcCrossChainServerTestBase
    {
        private readonly INodePlugin _grpcCrossChainServerNodePlugin;
        private readonly ChainOptions _chainOptions;
        
        public GrpcCrossChainServerNodePluginTest()
        {
            _grpcCrossChainServerNodePlugin = GetRequiredService<INodePlugin>();
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
        }

        [Fact]
        public async Task Server_Start_Test()
        {
            var chainId = _chainOptions.ChainId;
            await _grpcCrossChainServerNodePlugin.StartAsync(chainId);
        }
    }
}