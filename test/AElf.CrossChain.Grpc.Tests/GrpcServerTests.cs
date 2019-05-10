using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using Grpc.Core;
using Grpc.Core.Testing;
using Grpc.Core.Utils;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Grpc
{
    public class GrpcServerTests : GrpcCrossChainServerTestBase
    {
        private CrossChainRpc.CrossChainRpcBase CrossChainGrpcServer;
        private ISmartContractAddressService _smartContractAddressService;

        public GrpcServerTests()
        {
            CrossChainGrpcServer = GetRequiredService<CrossChainRpc.CrossChainRpcBase>();
            _smartContractAddressService = GetRequiredService<SmartContractAddressService>();
            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name, Address.Generate());
        }
        
        [Fact]
        public async Task RequestIndexingParentChain_WithoutExtraData()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = 0,
                NextHeight = 15
            };

            IServerStreamWriter<CrossChainResponse> responseStream = Mock.Of<IServerStreamWriter<CrossChainResponse>>();
            var context = BuildServerCallContext();
            await CrossChainGrpcServer.RequestIndexingFromParentChain(requestData, responseStream, context);
        }
        
        [Fact(Skip = "https://github.com/AElfProject/AElf/issues/1643")]
        public async Task RequestIndexingParentChain_WithExtraData()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = 0,
                NextHeight = 9
            };

            IServerStreamWriter<CrossChainResponse> responseStream = Mock.Of<IServerStreamWriter<CrossChainResponse>>();
            var context = BuildServerCallContext();
            await CrossChainGrpcServer.RequestIndexingFromParentChain(requestData, responseStream, context);
        }

        [Fact]
        public async Task RequestIndexingSideChain()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = 0,
                NextHeight = 10
            };
            
            IServerStreamWriter<CrossChainResponse> responseStream = Mock.Of<IServerStreamWriter<CrossChainResponse>>();
            var context = BuildServerCallContext();
            await CrossChainGrpcServer.RequestIndexingFromSideChain(requestData, responseStream, context);
        }

        [Fact]
        public async Task CrossChainIndexingShake()
        {
            var request = new HandShake
            {
                ListeningPort = 2100,
                FromChainId =  0
            };
            var context = BuildServerCallContext();
            var indexingHandShakeReply = await CrossChainGrpcServer.CrossChainIndexingShake(request, context);
            
            indexingHandShakeReply.ShouldNotBeNull();
            indexingHandShakeReply.Result.ShouldBeTrue();
        }
        
        private ServerCallContext BuildServerCallContext(Metadata metadata = null)
        {
            var meta = metadata ?? new Metadata();
            return TestServerCallContext.Create("mock", null, DateTime.UtcNow.AddHours(1), meta, CancellationToken.None, 
                "ipv4:127.0.0.1:2100", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
        }
    }
}