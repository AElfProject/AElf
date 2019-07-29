using System;
using System.Threading.Tasks;
using Grpc.Core;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcClientTests : GrpcCrossChainClientTestBase
    {
        private const string Host = "localhost";
        private const int ListenPort = 2200;
        private IGrpcCrossChainServer _server;
        private BasicCrossChainRpc.BasicCrossChainRpcClient _basicClient;
        private GrpcCrossChainClientService _grpcCrossChainClientService;
        private GrpcCrossChainClientProvider _grpcCrossChainClientProvider;

        public GrpcClientTests()
        {
            _server = GetRequiredService<IGrpcCrossChainServer>();
            _grpcCrossChainClientService = GetRequiredService<GrpcCrossChainClientService>();
            _grpcCrossChainClientProvider = GetRequiredService<GrpcCrossChainClientProvider>();
        }

        [Fact]
        public async Task BasicCrossChainClient_TryHandShake_Test()
        {
            await InitServerAndClientAsync(5000);
            var result = await _basicClient.CrossChainHandShakeAsync(new HandShake
            {
                ListeningPort = ListenPort,
                FromChainId = 0,
                Host = Host
            });
            result.Success.ShouldBeTrue();
            Dispose();
        }

        [Fact]
        public async Task GetClient_Test()
        {
            var remoteChainId = ChainHelper.GetChainId(1);
            var localChainId = ChainHelper.GetChainId(2);
            await _server.StartAsync(Host, 5000);
            CreateAndCacheClient(localChainId, false, 5000, remoteChainId);
            var client = await _grpcCrossChainClientProvider.GetClientAsync(remoteChainId);
            Assert.NotNull(client);
            Dispose();
        }

        [Fact]
        public async Task GetClientService_Test()
        {
            var remoteChainId = ChainHelper.GetChainId(1);
            var localChainId = ChainHelper.GetChainId(2);
            await _server.StartAsync(Host, 5000);

            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = Host,
                RemoteServerPort = 5000
            };

            await _grpcCrossChainClientService.CreateClientAsync(fakeCrossChainClient);
            var client = await _grpcCrossChainClientService.GetClientAsync(remoteChainId);
            Assert.NotNull(client);
            Dispose();
        }

        [Fact]
        public async Task RequestChainInitializationData_SideClient_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            await _server.StartAsync(Host, 5000);
            var client = CreateCrossChainClient(chainId, false);
            await Assert.ThrowsAsync<NotImplementedException>(() =>
                client.RequestChainInitializationDataAsync(chainId));
            Dispose();
        }

        [Fact]
        public async Task RequestChainInitializationData_ParentClient_Test()
        {
            var chainId = _chainOptions.ChainId;
            await _server.StartAsync(Host, 5000);
            var client = _grpcCrossChainClientService.CreateClientForChainInitializationData(chainId);
            await client.RequestChainInitializationDataAsync(chainId);
            Dispose();
        }

        [Fact]
        public async Task RequestCrossChainData_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var height = 2;
            await _server.StartAsync(Host, 5000);
            var client = CreateCrossChainClient(chainId, false);
            await client.RequestCrossChainDataAsync(height);
        }

        private async Task InitServerAndClientAsync(int port)
        {
            await _server.StartAsync(Host, port);
            _basicClient =
                new BasicCrossChainRpc.BasicCrossChainRpcClient(new Channel(Host, port, ChannelCredentials.Insecure));
        }

        private void CreateAndCacheClient(int chainId, bool toParenChain, int port, int remoteChainId = 0)
        {
            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = chainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = toParenChain,
                RemoteServerHost = Host,
                RemoteServerPort = port
            };
            _grpcCrossChainClientProvider.CreateAndCacheClient(fakeCrossChainClient);
        }

        private ICrossChainClient CreateCrossChainClient(int chainId, bool toParenChain, int remoteChainId = 0)
        {
            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = chainId,
                RemoteChainId = 0,
                IsClientToParentChain = toParenChain,
                RemoteServerHost = Host,
                RemoteServerPort = 5000
            };
            var res = _grpcCrossChainClientProvider.CreateCrossChainClient(fakeCrossChainClient);
            return res;
        }

        public override void Dispose()
        {
            _server?.Dispose();
        }
    }
}