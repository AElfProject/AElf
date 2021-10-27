using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Grpc.Client
{
    public sealed class GrpcCrossChainClientNodePluginTests : GrpcCrossChainClientTestBase
    {
        private readonly IGrpcClientPlugin _grpcCrossChainClientNodePlugin;
        private readonly ICrossChainClientProvider _crossChainClientProvider;

        public GrpcCrossChainClientNodePluginTests()
        {
            _grpcCrossChainClientNodePlugin = GetRequiredService<IGrpcClientPlugin>();
            _crossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
        }

        [Fact]
        public async Task ClientStart_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var remoteChainId = ChainOptions.ChainId;
            await Server.StartAsync(5000);
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
            var clients = _crossChainClientProvider.GetAllClients();
            clients[0].RemoteChainId.ShouldBe(remoteChainId);
            Server.Dispose();
        }
    }
    
    public sealed class GrpcCrossChainClientNodePluginWithoutParentChainTests : GrpcCrossChainClientWithoutParentChainTestBase
    {
        private readonly IGrpcClientPlugin _grpcCrossChainClientNodePlugin;
        private readonly ICrossChainClientProvider _crossChainClientProvider;

        public GrpcCrossChainClientNodePluginWithoutParentChainTests()
        {
            _grpcCrossChainClientNodePlugin = GetRequiredService<IGrpcClientPlugin>();
            _crossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
        }

        [Fact]
        public async Task ClientWithoutParentChainStart_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            await Server.StartAsync(5000);
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
            var clients = _crossChainClientProvider.GetAllClients();
            clients.ShouldBeEmpty();
            Server.Dispose();
        }
    }
}