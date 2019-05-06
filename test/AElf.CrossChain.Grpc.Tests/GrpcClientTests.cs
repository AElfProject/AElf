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
        private ICrossChainDataProducer _crossChainDataProducer;
        
        public GrpcClientTests()
        {
            _server = GetRequiredService<ICrossChainServer>();
            _certificateStore = GetRequiredService<ICertificateStore>();
            _crossChainDataProducer = GetRequiredService<ICrossChainDataProducer>();
            
            InitServerAndClient();      
        }

        [Fact]
        public async Task ParentChainClient_StartIndexingRequest_WithException()
        {
            await Assert.ThrowsAsync<RpcException>(()=>parentClient.StartIndexingRequest(0, 1, _crossChainDataProducer));  
        }
        
        [Fact(Skip = "Not meaningful at all.")]
        public async Task SideChainClient_StartIndexingRequest_WithException()
        {
            // is this meaningful? 
            await Assert.ThrowsAsync<RpcException>(()=>sideClient.StartIndexingRequest(0, 2, _crossChainDataProducer));
        }

        [Fact]
        public async Task ParentChainClient_TryHandShakeAsync()
        {
            var result = await parentClient.HandShakeAsync(0, ListenPort);
            result.Result.ShouldBeTrue();

            parentClient = new GrpcClientForParentChain("localhost:3000", 0,1);
            await Assert.ThrowsAsync<RpcException>(()=>parentClient.HandShakeAsync(0, 3000));
        }
        
        [Fact]
        public async Task SideChainClient_TryHandShakeAsync()
        {
            var result = await sideClient.HandShakeAsync(0, ListenPort);
            result.Result.ShouldBeTrue();

            sideClient = new GrpcClientForSideChain("localhost:3000", 1);
            await Assert.ThrowsAsync<RpcException>(()=>sideClient.HandShakeAsync(0, 3000));
        }

        private void InitServerAndClient()
        {
            var keyStore = _certificateStore.LoadKeyStore("test");
            var cert = _certificateStore.LoadCertificate("test");
            var keyCert = new KeyCertificatePair(cert, keyStore);
            
            _server.StartAsync(Host, ListenPort).Wait();
            
            string uri = $"{Host}:{ListenPort}";
            parentClient = new GrpcClientForParentChain(uri, 0, 1);
            sideClient = new GrpcClientForSideChain(uri, 1);
        }

        public override void Dispose()
        {
            _server?.Dispose();
        }
    }
}