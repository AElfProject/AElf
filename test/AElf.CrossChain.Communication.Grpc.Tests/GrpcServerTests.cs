using System;
using System.Threading;
using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Grpc.Core;
using Grpc.Core.Testing;
using Grpc.Core.Utils;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcServerTests : GrpcCrossChainServerTestBase
    {
        private ParentChainRpc.ParentChainRpcBase ParentChainGrpcServerBase;
        private SideChainRpc.SideChainRpcBase SideChainGrpcServerBase;
        private BasicCrossChainRpc.BasicCrossChainRpcBase BasicCrossChainRpcBase;

        private ISmartContractAddressService _smartContractAddressService;

        public GrpcServerTests()
        {
            ParentChainGrpcServerBase = GetRequiredService<GrpcParentChainServerBase>();
            SideChainGrpcServerBase = GetRequiredService<GrpcSideChainServerBase>();
            BasicCrossChainRpcBase = GetRequiredService<GrpcBasicServerBase>();
            _smartContractAddressService = GetRequiredService<SmartContractAddressService>();
            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name, SampleAddress.AddressList[0]);
        }
        
        [Fact]
        public async Task RequestIndexingParentChain_WithoutExtraData()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = 0,
                NextHeight = 10
            };

            IServerStreamWriter<ParentChainBlockData> responseStream = Mock.Of<IServerStreamWriter<ParentChainBlockData>>();
            var context = BuildServerCallContext();
            await ParentChainGrpcServerBase.RequestIndexingFromParentChain(requestData, responseStream, context);
        }
        
        [Fact]
        public async Task RequestIndexingParentChain_WithExtraData()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = 0,
                NextHeight = 9
            };

            IServerStreamWriter<ParentChainBlockData> responseStream = Mock.Of<IServerStreamWriter<ParentChainBlockData>>();
            var context = BuildServerCallContext();
            await ParentChainGrpcServerBase.RequestIndexingFromParentChain(requestData, responseStream, context);
        }

        [Fact]
        public async Task RequestIndexingSideChain()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = 0,
                NextHeight = 10
            };
            
            IServerStreamWriter<SideChainBlockData> responseStream = Mock.Of<IServerStreamWriter<SideChainBlockData>>();
            var context = BuildServerCallContext();
            await SideChainGrpcServerBase.RequestIndexingFromSideChain(requestData, responseStream, context);
        }

        [Fact]
        public async Task CrossChainIndexingShake()
        {
            var request = new HandShake
            {
                ListeningPort = 2100,
                FromChainId = 0,
                Host = "127.0.0.1"
            };
            var context = BuildServerCallContext();
            var indexingHandShakeReply = await BasicCrossChainRpcBase.CrossChainHandShake(request, context);
            
            indexingHandShakeReply.ShouldNotBeNull();
            indexingHandShakeReply.Success.ShouldBeTrue();
        }
        
        private ServerCallContext BuildServerCallContext(Metadata metadata = null)
        {
            var meta = metadata ?? new Metadata();
            return TestServerCallContext.Create("mock", "127.0.0.1", TimestampHelper.GetUtcNow().AddHours(1).ToDateTime(), meta, CancellationToken.None, 
                "ipv4:127.0.0.1:2100", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(), writeOptions => { });
        }
    }
}