using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using Grpc.Core;
using Grpc.Core.Testing;
using Grpc.Core.Utils;
using Moq;
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
            _smartContractAddressService.SetAddress(CrossChainSmartContractAddressNameProvider.Name,
                SampleAddress.AddressList[0]);
        }

        [Fact]
        public async Task RequestIndexingParentChain_MaximalResponse_Test()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = ChainHelper.GetChainId(1),
                NextHeight = 10
            };

            var responseResults = new List<ParentChainBlockData>();
            IServerStreamWriter<ParentChainBlockData> responseStream = MockServerStreamWriter(responseResults); 
            var context = BuildServerCallContext();
            await ParentChainGrpcServerBase.RequestIndexingFromParentChain(requestData, responseStream, context);
            Assert.Equal(CrossChainCommunicationConstants.MaximalIndexingCount, responseResults.Count);
            Assert.Equal(10, responseResults[0].Height);
        }

        [Fact]
        public async Task RequestIndexingParentChain_EmptyResponse_Test()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = ChainHelper.GetChainId(1),
                NextHeight = 101
            };

            var responseResults = new List<ParentChainBlockData>();
            IServerStreamWriter<ParentChainBlockData> responseStream = MockServerStreamWriter(responseResults); 
            var context = BuildServerCallContext();
            await ParentChainGrpcServerBase.RequestIndexingFromParentChain(requestData, responseStream, context);
            Assert.Empty(responseResults);
        }
        
        [Fact]
        public async Task RequestIndexingParentChain_SpecificResponse_Test()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = ChainHelper.GetChainId(1),
                NextHeight = 61
            };

            var responseResults = new List<ParentChainBlockData>();
            IServerStreamWriter<ParentChainBlockData> responseStream = MockServerStreamWriter(responseResults); 
            var context = BuildServerCallContext();
            await ParentChainGrpcServerBase.RequestIndexingFromParentChain(requestData, responseStream, context);
            Assert.Equal(40, responseResults.Count);
            Assert.Equal(61, responseResults.First().Height);
            Assert.Equal(100, responseResults.Last().Height);
        }

        [Fact]
        public async Task RequestIndexingSideChain_MaximalResponse_Test()
        {
            var requestData = new CrossChainRequest
            {
                FromChainId = ChainHelper.GetChainId(1),
                NextHeight = 10
            };

            var responseResults = new List<SideChainBlockData>();
            IServerStreamWriter<SideChainBlockData> responseStream = MockServerStreamWriter(responseResults); 
            var context = BuildServerCallContext();
            await SideChainGrpcServerBase.RequestIndexingFromSideChain(requestData, responseStream, context);
            Assert.Equal(CrossChainCommunicationConstants.MaximalIndexingCount, responseResults.Count);
            Assert.Equal(10, responseResults[0].Height);
        }

        [Fact]
        public async Task CrossChainIndexingShake_Test()
        {
            var request = new HandShake
            {
                ListeningPort = 2100,
                FromChainId = ChainHelper.GetChainId(1),
                Host = "127.0.0.1"
            };
            var context = BuildServerCallContext();
            var indexingHandShakeReply = await BasicCrossChainRpcBase.CrossChainHandShake(request, context);

            Assert.NotNull(indexingHandShakeReply);
            Assert.True(indexingHandShakeReply.Success);
        }

        [Fact]
        public async Task RequestChainInitializationDataFromParentChain_Test()
        {
            var requestData = new SideChainInitializationRequest
            {
                ChainId = ChainHelper.GetChainId(1),
            };
            var context = BuildServerCallContext();
            var sideChainInitializationResponse =
                await ParentChainGrpcServerBase.RequestChainInitializationDataFromParentChain(requestData, context);
            Assert.Equal(1, sideChainInitializationResponse.CreationHeightOnParentChain);
        }

        private ServerCallContext BuildServerCallContext(Metadata metadata = null)
        {
            var meta = metadata ?? new Metadata();
            return TestServerCallContext.Create("mock", "127.0.0.1",
                TimestampHelper.GetUtcNow().AddHours(1).ToDateTime(), meta, CancellationToken.None,
                "ipv4:127.0.0.1:2100", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(),
                writeOptions => { });
        }

        private IServerStreamWriter<T> MockServerStreamWriter<T>(IList<T> list)
        {
            var mockServerStreamWriter = new Mock<IServerStreamWriter<T>>();
            mockServerStreamWriter.Setup(w => w.WriteAsync(It.IsAny<T>())).Returns<T>(o =>
            {
                list.Add(o);
                return Task.CompletedTask;
            });
            return mockServerStreamWriter.Object;
        } 
    }
}