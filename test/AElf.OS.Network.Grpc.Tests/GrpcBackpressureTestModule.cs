using System.Threading;
using System.Threading.Tasks;
using AElf.Modularity;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule), typeof(GrpcNetworkModule))]
    public class GrpcBackpressureTestModule : AElfModule
    {
        private GrpcPeer _peerUnderTest;
        private TaskCompletionSource<VoidReply> _testCompletionSource; // completion for all

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var mockClient = new Mock<PeerService.PeerServiceClient>();
            _testCompletionSource = new TaskCompletionSource<VoidReply>();

            // setup mock announcement stream
            var announcementStreamCall = MockStreamCall<BlockAnnouncement, VoidReply>(_testCompletionSource.Task);
            mockClient.Setup(m => m.AnnouncementBroadcastStream(null, null, CancellationToken.None))
                .Returns(announcementStreamCall);
            
            // setup mock transaction stream
            var transactionStreamCall = MockStreamCall<Transaction, VoidReply>(_testCompletionSource.Task);
            mockClient.Setup(m => m.TransactionBroadcastStream(null, null, CancellationToken.None))
                .Returns(transactionStreamCall);
            
            // setup mock block stream
            var blockStreamCall = MockStreamCall<BlockWithTransactions, VoidReply>(_testCompletionSource.Task);
            mockClient.Setup(m => m.BlockBroadcastStream(null, null, CancellationToken.None))
                .Returns(blockStreamCall);
            
            // create peer
            _peerUnderTest = GrpcTestPeerHelpers.CreatePeerWithClient(NetworkTestConstants.FakeIpEndpoint, 
               NetworkTestConstants.FakePubkey, mockClient.Object);
            
            _peerUnderTest.IsConnected = true;
            
            context.Services.AddSingleton<IPeer>(_peerUnderTest);
        }

        private AsyncClientStreamingCall<TReq, TResp> MockStreamCall<TReq, TResp>(Task<TResp> replyTask) where TResp : new()
        {
            var mockRequestStream = new Mock<IClientStreamWriter<TReq>>();
            mockRequestStream.Setup(m => m.WriteAsync(It.IsAny<TReq>()))
                .Returns(replyTask);
            
            var call = TestCalls.AsyncClientStreamingCall<TReq, TResp>(mockRequestStream.Object, Task.FromResult(new TResp()),
                Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });

            return call;
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            _testCompletionSource.SetCanceled();
            AsyncHelper.RunSync(async () => await _peerUnderTest.DisconnectAsync(false));
        }
    }
}