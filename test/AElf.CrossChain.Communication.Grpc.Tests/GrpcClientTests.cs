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
        private BasicCrossChainRpc.BasicCrossChainRpcClient _basicClient;
        
        private IGrpcCrossChainServer _server;
        
        public GrpcClientTests()
        {
            _server = GetRequiredService<IGrpcCrossChainServer>();
        }

        // TODO: These cases are meaningless and should be rewritten.
//        [Fact]
//        public async Task ParentChainClient_StartIndexingRequest_WithException()
//        {
//            await Assert.ThrowsAsync<RpcException>(()=>parentClient.StartIndexingRequest(0, 1, _crossChainDataProducer));  
//        }
//        
//        [Fact(Skip = "Not meaningful at all.")]
//        public async Task SideChainClient_StartIndexingRequest_WithException()
//        {
//            // is this meaningful? 
//            await Assert.ThrowsAsync<RpcException>(()=>sideClient.StartIndexingRequest(0, 2, _crossChainDataProducer));
//        }

        [Fact]
        public async Task BasicCrossChainClient_TryHandShake()
        {
            InitServerAndClient(5000);
            var result = await _basicClient.CrossChainHandShakeAsync(new HandShake
            {
                ListeningPort = 2100,
                FromChainId = 0,
                Host = "127.0.0.1"
            });
            result.Success.ShouldBeTrue();
            Dispose();
        }
        
        private void InitServerAndClient(int port)
        {
            _server.StartAsync(Host, port).Wait();
            _basicClient = new BasicCrossChainRpc.BasicCrossChainRpcClient(new Channel(Host, port, ChannelCredentials.Insecure));
        }

        public override void Dispose()
        {
            _server?.Dispose();
        }
    }
}