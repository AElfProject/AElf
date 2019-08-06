using System;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Communication.Application;
using AElf.CrossChain.Communication.Infrastructure;
using Grpc.Core;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcClientTests : GrpcCrossChainClientTestBase
    {
        private const string Host = "localhost";
        private const int ListenPort = 2200;
        private IGrpcCrossChainServer _server;
        private ICrossChainClientService _crossChainClientService;
        private BasicCrossChainRpc.BasicCrossChainRpcClient _basicClient;
        private GrpcCrossChainCommunicationTestHelper _grpcCrossChainCommunicationTestHelper;
        private ICrossChainClientProvider _grpcCrossChainClientProvider;

        public GrpcClientTests()
        {
            _server = GetRequiredService<IGrpcCrossChainServer>();
            _crossChainClientService = GetRequiredService<ICrossChainClientService>();
            _grpcCrossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
            _grpcCrossChainCommunicationTestHelper = GetRequiredService<GrpcCrossChainCommunicationTestHelper>();
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
            Assert.True(result.Success);
            Dispose();
        }

        [Fact]
        public async Task CreateAndCacheClient_Test()
        {
            var remoteChainId = _chainOptions.ChainId;
            var localChainId = ChainHelper.GetChainId(1);
            await _server.StartAsync(Host, 5000);

            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = Host,
                RemoteServerPort = 5000
            };
            
            _grpcCrossChainClientProvider.AddOrUpdateClient(fakeCrossChainClient);
            var getClient = _grpcCrossChainClientProvider.TryGetClient(remoteChainId, out _);
            Assert.True(getClient);
            var client = _grpcCrossChainClientProvider.GetAllClients();
            Assert.True(client[0].RemoteChainId == remoteChainId);
            Assert.True(client[0].TargetUriString.Equals("localhost:5000"));
            Dispose();
        }
        
        [Fact]
        public async Task CreateCrossChainClient_Test()
        {
            var remoteChainId = _chainOptions.ChainId;
            var localChainId = ChainHelper.GetChainId(1);
            await _server.StartAsync(Host, 5000);

            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = Host,
                RemoteServerPort = 5000
            };
            
            var client = _grpcCrossChainClientProvider.CreateCrossChainClient(fakeCrossChainClient); 
            Assert.True(client.RemoteChainId == remoteChainId);
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
            var chainId = ChainHelper.GetChainId(1);
            await _server.StartAsync(Host, 5000);
            var client = CreateCrossChainClient(chainId,true);
            var res = await client.RequestChainInitializationDataAsync(chainId);
            Assert.True(res.CreationHeightOnParentChain == 1);
            Dispose();
        }

        [Fact]
        public async Task RequestCrossChainData_Test()
        {
            var localChainId = ChainHelper.GetChainId(1);
            var remoteChainId = _chainOptions.ChainId;
            var height = 2;
            await _server.StartAsync(Host, 5000);
            var client = CreateCrossChainClient(localChainId, false, remoteChainId);
            _grpcCrossChainCommunicationTestHelper.GrpcCrossChainClients.TryAdd(remoteChainId, client);
            await client.ConnectAsync();
            _grpcCrossChainCommunicationTestHelper.FakeSideChainBlockDataEntityCacheOnServerSide(height);
            await client.RequestCrossChainDataAsync(height);
            
            var clientBlockDataEntityCache = GrpcCrossChainCommunicationTestHelper.ClientBlockDataEntityCache;
            var sideChainBlockData = new SideChainBlockData {Height = height};
            Assert.True(clientBlockDataEntityCache.Contains(sideChainBlockData));
            Dispose();
        }

        [Fact]
        public async Task CloseClient_Test()
        {
            var remoteChainId = _chainOptions.ChainId;
            var localChainId = ChainHelper.GetChainId(1);
            await _server.StartAsync(Host, 5000);

            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = Host,
                RemoteServerPort = 5000
            };

            await _crossChainClientService.CreateClientAsync(fakeCrossChainClient);
            var getClient = _grpcCrossChainClientProvider.TryGetClient(remoteChainId, out _);
            Assert.True(getClient);
            var client = _grpcCrossChainClientProvider.GetAllClients();
            await client[0].ConnectAsync();
            Assert.True(client[0].IsConnected);
            await _crossChainClientService.CloseClientsAsync();
            Assert.False(client[0].IsConnected);
            Dispose();
        }
        
        private async Task InitServerAndClientAsync(int port)
        {
            await _server.StartAsync(Host, port);
            _basicClient =
                new BasicCrossChainRpc.BasicCrossChainRpcClient(new Channel(Host, port, ChannelCredentials.Insecure));
        }

        private ICrossChainClient CreateCrossChainClient(int chainId, bool toParenChain, int remoteChainId = 0)
        {
            var fakeCrossChainClient = new CrossChainClientDto
            {
                LocalChainId = chainId,
                RemoteChainId = remoteChainId,
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