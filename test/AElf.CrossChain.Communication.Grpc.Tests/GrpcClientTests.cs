using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain.Cache.Application;
using Grpc.Core;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcClientTests : GrpcCrossChainClientTestBase
    {
        private const string Host = "localhost";
        private const int ListenPort = 2200;
        private BasicCrossChainRpc.BasicCrossChainRpcClient _basicClient;
        private GrpcCrossChainCommunicationTestHelper _grpcCrossChainCommunicationTestHelper;
        private readonly IBlockCacheEntityProducer _blockCacheEntityProducer;

        public GrpcClientTests()
        {
            _grpcCrossChainCommunicationTestHelper = GetRequiredService<GrpcCrossChainCommunicationTestHelper>();
            _blockCacheEntityProducer = GetRequiredService<IBlockCacheEntityProducer>();
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
        public async Task RequestChainInitializationData_ParentClient_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            await Server.StartAsync(Host, 5000);

            var grpcClientInitializationContext = new GrpcClientInitializationContext
            {
                LocalChainId = chainId,
                RemoteChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
                DialTimeout = 1000,
                UriStr = string.Concat(Host, ":", "5000")
            };
            var client = new ClientForParentChain(grpcClientInitializationContext);
            var res = await client.RequestChainInitializationDataAsync(chainId);
            Assert.Equal(1, res.CreationHeightOnParentChain);
            Dispose();
        }

        [Fact]
        public async Task RequestCrossChainData_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var remoteChainId = ChainOptions.ChainId;
            var height = 2;
            var port = 5000;
            await Server.StartAsync(Host, port);
            
            var grpcClientInitializationContext = new GrpcClientInitializationContext
            {
                RemoteChainId = chainId,
                LocalChainId = ChainHelper.ConvertBase58ToChainId("AELF"),
                DialTimeout = 1000,
                UriStr = string.Concat(Host, ":", port)
            };
            var client = new ClientForSideChain(grpcClientInitializationContext);
            _grpcCrossChainCommunicationTestHelper.GrpcCrossChainClients.TryAdd(remoteChainId, client);
            _grpcCrossChainCommunicationTestHelper.FakeSideChainBlockDataEntityCacheOnServerSide(height);
            await client.RequestCrossChainDataAsync(height, b => _blockCacheEntityProducer.TryAddBlockCacheEntity(b));
            
            var clientBlockDataEntityCache = GrpcCrossChainCommunicationTestHelper.ClientBlockDataEntityCache;
            var sideChainBlockData = new SideChainBlockData {Height = height};
            Assert.Contains(sideChainBlockData, clientBlockDataEntityCache);
            Dispose();
        }

        private async Task InitServerAndClientAsync(int port)
        {
            await Server.StartAsync(Host, port);
            _basicClient =
                new BasicCrossChainRpc.BasicCrossChainRpcClient(new Channel(Host, port, ChannelCredentials.Insecure));
        }

        public override void Dispose()
        {
            Server?.Dispose();
        }
    }
}