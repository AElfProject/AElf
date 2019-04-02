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
            await Assert.ThrowsAsync<RpcException>(()=>parentClient.StartIndexingRequest(0, _crossChainDataProducer));  
        }
        
        [Fact]
        public async Task SideChainClient_StartIndexingRequest_WithException()
        {
            await Assert.ThrowsAsync<RpcException>(()=>sideClient.StartIndexingRequest(0, _crossChainDataProducer));
        }

        [Fact]
        public async Task ParentChainClient_TryHandShakeAsync()
        {
            var result = await parentClient.TryHandShakeAsync(0, ListenPort);
            result.Result.ShouldBeTrue();

            parentClient = new GrpcClientForParentChain("localhost:3000", "test", 0);
            await Assert.ThrowsAsync<RpcException>(()=>parentClient.TryHandShakeAsync(0, 3000));
        }
        
        [Fact]
        public async Task SideChainClient_TryHandShakeAsync()
        {
            var result = await sideClient.TryHandShakeAsync(0, ListenPort);
            result.Result.ShouldBeTrue();

            sideClient = new GrpcClientForSideChain("localhost:3000", "test");
            await Assert.ThrowsAsync<RpcException>(()=>sideClient.TryHandShakeAsync(0, 3000));
        }

        private void InitServerAndClient()
        {
            var keyStore = _certificateStore.LoadKeyStore("test");
            var cert = _certificateStore.LoadCertificate("test");
            var keyCert = new KeyCertificatePair(cert, keyStore);
            
            _server.StartAsync(Host, ListenPort, keyCert).Wait();
            
            string uri = $"{Host}:{ListenPort}";
            parentClient = new GrpcClientForParentChain(uri, cert, 0);
            sideClient = new GrpcClientForSideChain(uri, cert);
        }

        public override void Dispose()
        {
            _server?.Dispose();
        }
    }
}