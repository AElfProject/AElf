using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Grpc.Core;
using Grpc.Core.Testing;
using Moq;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.OS.Network.Grpc
{
    public class GrpcPeerTests : GrpcNetworkWithChainTestBase
    {
        private IPeerPool _pool;
        private GrpcPeer _grpcPeer;
        private GrpcPeer _nonInterceptedPeer;

        public GrpcPeerTests()
        {
            _pool = GetRequiredService<IPeerPool>();

            _grpcPeer = GrpcTestPeerHelper.CreateNewPeer();
            _grpcPeer.IsConnected = true;
            _nonInterceptedPeer = MockServiceClient("127.0.0.1:2000");

            _pool.TryAddPeer(_grpcPeer);
        }

        [Fact]
        public void KnowsBlock_Test()
        {
            var hash = HashHelper.ComputeFrom("TestHash");
            
            _grpcPeer.KnowsBlock(hash).ShouldBeFalse();
            _grpcPeer.TryAddKnownBlock(hash).ShouldBeTrue();
            _grpcPeer.KnowsBlock(hash).ShouldBeTrue();
            _grpcPeer.TryAddKnownBlock(hash).ShouldBeFalse();
        }
        
        [Fact]
        public void KnowsTransaction_Test()
        {
            var hash = HashHelper.ComputeFrom("TransactionHash");
            
            _grpcPeer.KnowsTransaction(hash).ShouldBeFalse();
            _grpcPeer.TryAddKnownTransaction(hash).ShouldBeTrue();
            _grpcPeer.KnowsTransaction(hash).ShouldBeTrue();
            _grpcPeer.TryAddKnownTransaction(hash).ShouldBeFalse();
        }

        [Fact]
        public void UpdateLastKnownLib_Test()
        {
            var libAnnouncement = new LibAnnouncement
            {
                LibHeight = 100,
                LibHash = HashHelper.ComputeFrom(100)
            };
            _grpcPeer.UpdateLastKnownLib(libAnnouncement);
            _grpcPeer.LastKnownLibHash.ShouldBe(libAnnouncement.LibHash);
            _grpcPeer.LastKnownLibHeight.ShouldBe(libAnnouncement.LibHeight);
            
            libAnnouncement = new LibAnnouncement
            {
                LibHeight = 101,
                LibHash = HashHelper.ComputeFrom(101)
            };
            _grpcPeer.UpdateLastKnownLib(libAnnouncement);
            _grpcPeer.LastKnownLibHash.ShouldBe(libAnnouncement.LibHash);
            _grpcPeer.LastKnownLibHeight.ShouldBe(libAnnouncement.LibHeight);
            
            var wrongLibAnnouncement = new LibAnnouncement
            {
                LibHeight = 90,
                LibHash = HashHelper.ComputeFrom(90)
            };
            _grpcPeer.UpdateLastKnownLib(wrongLibAnnouncement);
            _grpcPeer.LastKnownLibHash.ShouldBe(libAnnouncement.LibHash);
            _grpcPeer.LastKnownLibHeight.ShouldBe(libAnnouncement.LibHeight);
        }

        [Fact]
        public void EnqueueBlock_ShouldExecuteCallback_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            bool called = false;
            _nonInterceptedPeer.EnqueueBlock(new BlockWithTransactions(), ex =>
            {
                exception = ex;
                called = true;
                executed.Set();
            });

            executed.WaitOne();
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public void EnqueueTransaction_ShouldExecuteCallback_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            var transaction = new Transaction();
            bool called = false;
            _nonInterceptedPeer.EnqueueTransaction(transaction, ex =>
            {
                exception = ex;
                called = true;
                executed.Set();
            });

            executed.WaitOne();
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public void EnqueueAnnouncement_ShouldExecuteCallback_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            var called = false;
            _nonInterceptedPeer.EnqueueAnnouncement(new BlockAnnouncement(), ex =>
            {
                exception = ex;
                called = true;
                executed.Set();
            });

            executed.WaitOne();
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }
        
        [Fact]
        public void EnqueueLibAnnouncement_ShouldExecuteCallback_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            bool called = false;
            _nonInterceptedPeer.EnqueueLibAnnouncement(new LibAnnouncement(), ex =>
            {
                exception = ex;
                called = true;
                executed.Set();
            });

            executed.WaitOne();
            exception.ShouldBeNull();
            called.ShouldBeTrue();
        }

        [Fact]
        public void EnqueueAnnouncement_WithPeerNotReady_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            _nonInterceptedPeer.IsConnected = false;
            Should.Throw<NetworkException>(() =>
                _nonInterceptedPeer.EnqueueAnnouncement(new BlockAnnouncement(), ex =>
                {
                    exception = ex;
                    executed.Set();
                }));
        }
        
        [Fact]
        public void EnqueueBlock_WithPeerNotReady_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            _nonInterceptedPeer.IsConnected = false;

            Should.Throw<NetworkException>(()=>
                _nonInterceptedPeer.EnqueueBlock(new BlockWithTransactions(), ex =>
                {
                    exception = ex;
                    executed.Set();
                }));
        }
        
        [Fact]
        public void EnqueueTransaction_WithPeerNotReady_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            _nonInterceptedPeer.IsConnected = false;

            Should.Throw<NetworkException>(()=>
                _nonInterceptedPeer.EnqueueTransaction(new Transaction(), ex =>
                {
                    exception = ex;
                    executed.Set();
                }));
        }
        
        [Fact]
        public void EnqueueLibAnnouncement_WithPeerNotReady_Test()
        {
            AutoResetEvent executed = new AutoResetEvent(false);

            NetworkException exception = null;
            _nonInterceptedPeer.IsConnected = false;

            Should.Throw<NetworkException>(()=>
                _nonInterceptedPeer.EnqueueLibAnnouncement(new LibAnnouncement(), ex =>
                {
                    exception = ex;
                    executed.Set();
                }));
        }

        [Fact]
        public void GetRequestMetrics_Test()
        {
            var result = _grpcPeer.GetRequestMetrics();
            
            result.Count.ShouldBe(3);
            result.Keys.ShouldContain("GetBlock");
            result.Keys.ShouldContain("GetBlocks");
            result.Keys.ShouldContain("Announce");
        }
        
        [Fact]
        public async Task DisconnectAsync_Test()
        {
            var isReady = _grpcPeer.IsReady;
            isReady.ShouldBeTrue();
            
            await _grpcPeer.DisconnectAsync(false);
            
            _grpcPeer.IsShutdown.ShouldBeTrue();
            _grpcPeer.IsConnected.ShouldBeFalse();
            _grpcPeer.ConnectionStatus.ShouldBe("Shutdown");
        }

        [Fact]
        public async Task CheckHealth_Test()
        {
            var mockClient = new Mock<PeerService.PeerServiceClient>();
            mockClient.Setup(c =>
                    c.CheckHealthAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), null,
                        CancellationToken.None))
                .Returns(MockAsyncUnaryCall<HealthCheckReply>(new HealthCheckReply()));
            var grpcPeer = CreatePeer(mockClient.Object);
            await grpcPeer.CheckHealthAsync();
        }
        
        [Fact]
        public async Task CheckHealth_ObjectDisposedException_Test()
        {
            var mockClient = new Mock<PeerService.PeerServiceClient>();
            mockClient.Setup(c =>
                    c.CheckHealthAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), null, CancellationToken.None))
                .Throws(new ObjectDisposedException("ObjectDisposedException"));
            var grpcPeer = CreatePeer(mockClient.Object);
            grpcPeer.CheckHealthAsync()
                .ShouldThrow<NetworkException>().ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
        }
        
        [Fact]
        public async Task CheckHealth_RpcException_Test()
        {
            var mockClient = new Mock<PeerService.PeerServiceClient>();
            mockClient.Setup(c =>
                    c.CheckHealthAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), null,
                        CancellationToken.None))
                .Throws(new AggregateException(new RpcException(new Status(StatusCode.Cancelled, ""))));
            var grpcPeer = CreatePeer(mockClient.Object);

            grpcPeer.CheckHealthAsync()
                .ShouldThrow<NetworkException>().ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
            mockClient = new Mock<PeerService.PeerServiceClient>();
            mockClient.Setup(c =>
                    c.CheckHealthAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), null,
                        CancellationToken.None))
                .Throws(new AggregateException());
            grpcPeer = CreatePeer(mockClient.Object);

            grpcPeer.CheckHealthAsync()
                .ShouldThrow<NetworkException>().ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
        }

        private GrpcPeer CreatePeer(PeerService.PeerServiceClient client)
        {
            return GrpcTestPeerHelper.CreatePeerWithClient("127.0.0.1:2000", NetworkTestConstants.FakePubkey, client);
        }

        private GrpcPeer MockServiceClient(string ipAddress)
        {
            var mockClient = new Mock<PeerService.PeerServiceClient>();
            var testCompletionSource = Task.FromResult(new VoidReply());

            // setup mock announcement stream
            var announcementStreamCall = MockStreamCall<BlockAnnouncement, VoidReply>(testCompletionSource);
            mockClient.Setup(m => m.AnnouncementBroadcastStream(It.IsAny<Metadata>(), null, CancellationToken.None))
                .Returns(announcementStreamCall);
            
            // setup mock transaction stream
            var transactionStreamCall = MockStreamCall<Transaction, VoidReply>(testCompletionSource);
            mockClient.Setup(m => m.TransactionBroadcastStream(It.IsAny<Metadata>(), null, CancellationToken.None))
                .Returns(transactionStreamCall);
            
            // setup mock block stream
            var blockStreamCall = MockStreamCall<BlockWithTransactions, VoidReply>(testCompletionSource);
            mockClient.Setup(m => m.BlockBroadcastStream(It.IsAny<Metadata>(), null, CancellationToken.None))
                .Returns(blockStreamCall);
            
            // setup mock lib stream
            var libAnnouncementStreamCall = MockStreamCall<LibAnnouncement, VoidReply>(testCompletionSource);
            mockClient.Setup(m => m.LibAnnouncementBroadcastStream(It.IsAny<Metadata>(), null, CancellationToken.None))
                .Returns(libAnnouncementStreamCall);
            
            // create peer
            var grpcPeer = GrpcTestPeerHelper.CreatePeerWithClient(ipAddress, 
                NetworkTestConstants.FakePubkey, mockClient.Object);
            
            grpcPeer.IsConnected = true;

            return grpcPeer;
        }
        
        private AsyncClientStreamingCall<TReq, TResp> MockStreamCall<TReq, TResp>(Task<TResp> replyTask) where TResp : new()
        {
            var mockRequestStream = new Mock<IClientStreamWriter<TReq>>();
            mockRequestStream.Setup(m => m.WriteAsync(It.IsAny<TReq>()))
                .Returns(replyTask);
            
            var call = TestCalls.AsyncClientStreamingCall(mockRequestStream.Object, Task.FromResult(new TResp()),
                Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });

            return call;
        }
        
        private AsyncUnaryCall<TResponse> MockAsyncUnaryCall<TResponse>(TResponse reply) where TResponse : new()
        {
            var call = TestCalls.AsyncUnaryCall(Task.FromResult(reply),
                Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => { });

            return call;
        }
    }
}