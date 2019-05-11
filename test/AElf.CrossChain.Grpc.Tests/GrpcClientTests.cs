using System.Threading.Tasks;
using AElf.CrossChain.Cache;
using AElf.Cryptography.Certificate;
using Grpc.Core;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Grpc
{
    public class GrpcClientTests : GrpcCrossChainClientTestBase
    {
        private const string Host = "localhost";
        private const int ListenPort = 2200;
        private GrpcClientForParentChain parentClient;
        private GrpcClientForSideChain sideClient;
        
        private ICrossChainServer _server;
        private ICertificateStore _certificateStore;
        private IBlockCacheEntityProducer _blockCacheEntityProducer;
        
        public GrpcClientTests()
        {
            _server = GetRequiredService<ICrossChainServer>();
            _certificateStore = GetRequiredService<ICertificateStore>();
            _blockCacheEntityProducer = GetRequiredService<IBlockCacheEntityProducer>();
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
        public async Task ParentChainClient_TryHandShakeAsync()
        {
            InitServerAndClient(5000);
            var result = await parentClient.HandShakeAsync(0, ListenPort);
            result.Result.ShouldBeTrue();
            Dispose();
        }
        
        [Fact]
        public async Task SideChainClient_TryHandShakeAsync()
        {
            InitServerAndClient(6000);
            var result = await sideClient.HandShakeAsync(0, ListenPort);
            result.Result.ShouldBeTrue();
            Dispose();
        }

        private void InitServerAndClient(int port)
        {
            _server.StartAsync(Host, port).Wait();
            
            string uri = $"{Host}:{port}";
            parentClient = new GrpcClientForParentChain(uri, 0, 1);
            sideClient = new GrpcClientForSideChain(uri, 1, 1);
        }

        public override void Dispose()
        {
            _server?.Dispose();
        }
    }
}