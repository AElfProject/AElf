using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
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
            var requestData = new RequestCrossChainBlockData
            {
                FromChainId = 0,
                NextHeight = 15
            };

            IServerStreamWriter<ResponseParentChainBlockData> responseStream = Mock.Of<IServerStreamWriter<ResponseParentChainBlockData>>();
            var context = BuildServerCallContext();
            await CrossChainGrpcServer.RequestIndexingParentChain(requestData, responseStream, context);
        }
        
        [Fact]
        public async Task RequestIndexingParentChain_WithExtraData()
        {
            var requestData = new RequestCrossChainBlockData
            {
                FromChainId = 0,
                NextHeight = 9
            };

            IServerStreamWriter<ResponseParentChainBlockData> responseStream = Mock.Of<IServerStreamWriter<ResponseParentChainBlockData>>();
            var context = BuildServerCallContext();
            await CrossChainGrpcServer.RequestIndexingParentChain(requestData, responseStream, context);
        }

        [Fact]
        public async Task RequestIndexingSideChain()
        {
            var requestData = new RequestCrossChainBlockData
            {
                FromChainId = 0,
                NextHeight = 10
            };
            
            IServerStreamWriter<ResponseSideChainBlockData> responseStream = Mock.Of<IServerStreamWriter<ResponseSideChainBlockData>>();
            var context = BuildServerCallContext();
            await CrossChainGrpcServer.RequestIndexingSideChain(requestData, responseStream, context);
        }

        [Fact]
        public async Task CrossChainIndexingShake()
        {
            var request = new IndexingHandShake
            {
                ListeningPort = 2100,
                ChainId = 0
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