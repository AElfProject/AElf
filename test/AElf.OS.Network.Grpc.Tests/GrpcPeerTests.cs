using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Testing;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Grpc;

public class GrpcPeerTests : GrpcNetworkWithChainTestBase
{
    private readonly OSTestHelper _osTestHelper;
    private readonly IPeerPool _pool;

    private readonly GrpcPeer _grpcPeer;
    private readonly GrpcPeer _nonInterceptedPeer;

    public GrpcPeerTests()
    {
        _osTestHelper = GetRequiredService<OSTestHelper>();
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
        var executed = new AutoResetEvent(false);

        NetworkException exception = null;
        var called = false;
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
        var executed = new AutoResetEvent(false);

        NetworkException exception = null;
        var transaction = new Transaction();
        var called = false;
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
        var executed = new AutoResetEvent(false);

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
        var executed = new AutoResetEvent(false);

        NetworkException exception = null;
        var called = false;
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
        var executed = new AutoResetEvent(false);

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
        var executed = new AutoResetEvent(false);

        NetworkException exception = null;
        _nonInterceptedPeer.IsConnected = false;

        Should.Throw<NetworkException>(() =>
            _nonInterceptedPeer.EnqueueBlock(new BlockWithTransactions(), ex =>
            {
                exception = ex;
                executed.Set();
            }));
    }

    [Fact]
    public void EnqueueTransaction_WithPeerNotReady_Test()
    {
        var executed = new AutoResetEvent(false);

        NetworkException exception = null;
        _nonInterceptedPeer.IsConnected = false;

        Should.Throw<NetworkException>(() =>
            _nonInterceptedPeer.EnqueueTransaction(new Transaction(), ex =>
            {
                exception = ex;
                executed.Set();
            }));
    }

    [Fact]
    public void EnqueueLibAnnouncement_WithPeerNotReady_Test()
    {
        var executed = new AutoResetEvent(false);

        NetworkException exception = null;
        _nonInterceptedPeer.IsConnected = false;

        Should.Throw<NetworkException>(() =>
            _nonInterceptedPeer.EnqueueLibAnnouncement(new LibAnnouncement(), ex =>
            {
                exception = ex;
                executed.Set();
            }));
    }

    [Fact]
    public async Task DisconnectAsync_Test()
    {
        _grpcPeer.IsReady.ShouldBeTrue();
        _grpcPeer.IsShutdown.ShouldBeFalse();
        _grpcPeer.IsConnected.ShouldBeTrue();
        _grpcPeer.IsInvalid.ShouldBeFalse();

        await _grpcPeer.DisconnectAsync(false);

        _grpcPeer.IsShutdown.ShouldBeTrue();
        _grpcPeer.IsConnected.ShouldBeFalse();
        _grpcPeer.IsReady.ShouldBeFalse();
        _grpcPeer.IsInvalid.ShouldBeFalse();
        _grpcPeer.ConnectionStatus.ShouldBe("Shutdown");

        _grpcPeer.Info.ConnectionTime = TimestampHelper.GetUtcNow().AddSeconds(-11);
        _grpcPeer.IsInvalid.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckHealth_Test()
    {
        var mockClient = new Mock<PeerService.PeerServiceClient>();
        mockClient.Setup(c =>
                c.CheckHealthAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), null,
                    CancellationToken.None))
            .Returns(MockAsyncUnaryCall(new HealthCheckReply()));
        var grpcPeer = CreatePeer(mockClient.Object);
        await grpcPeer.CheckHealthAsync();
    }

    [Fact]
    public async Task GetBlockByHash_Test()
    {
        var block = new BlockWithTransactions
            { Header = _osTestHelper.GenerateBlock(HashHelper.ComputeFrom("PreBlockHash"), 100).Header };

        var mockClient = new Mock<PeerService.PeerServiceClient>();
        mockClient.Setup(c =>
                c.RequestBlockAsync(It.IsAny<BlockRequest>(), It.IsAny<Metadata>(), null,
                    CancellationToken.None))
            .Returns(MockAsyncUnaryCall(new BlockReply { Block = block }));
        var grpcPeer = CreatePeer(mockClient.Object);

        var result = await grpcPeer.GetBlockByHashAsync(block.GetHash());
        result.ShouldBe(block);

        var metrics = grpcPeer.GetRequestMetrics();
        metrics["GetBlock"].Count.ShouldBe(1);
        metrics["GetBlock"][0].MethodName.ShouldContain("GetBlock");
        metrics["GetBlock"][0].Info.ShouldContain("Block request for");
    }

    [Fact]
    public async Task GetBlocks_Test()
    {
        var block = new BlockWithTransactions
            { Header = _osTestHelper.GenerateBlock(HashHelper.ComputeFrom("PreBlockHash"), 100).Header };
        var blockList = new BlockList();
        blockList.Blocks.Add(block);

        var mockClient = new Mock<PeerService.PeerServiceClient>();
        mockClient.Setup(c =>
                c.RequestBlocksAsync(It.IsAny<BlocksRequest>(), It.IsAny<Metadata>(), null,
                    CancellationToken.None))
            .Returns(MockAsyncUnaryCall(blockList));
        var grpcPeer = CreatePeer(mockClient.Object);

        var result = await grpcPeer.GetBlocksAsync(block.Header.PreviousBlockHash, 1);
        result.ShouldBe(blockList.Blocks);

        var metrics = grpcPeer.GetRequestMetrics();
        metrics["GetBlocks"].Count.ShouldBe(1);
        metrics["GetBlocks"][0].MethodName.ShouldContain("GetBlocks");
        metrics["GetBlocks"][0].Info.ShouldContain("Get blocks for");
    }

    [Fact]
    public async Task GetNodes_Test()
    {
        var nodeList = new NodeList();
        nodeList.Nodes.Add(new NodeInfo { Endpoint = "127.0.0.1:123", Pubkey = ByteString.Empty });

        var mockClient = new Mock<PeerService.PeerServiceClient>();
        mockClient.Setup(c =>
                c.GetNodesAsync(It.IsAny<NodesRequest>(), It.IsAny<Metadata>(), null,
                    CancellationToken.None))
            .Returns(MockAsyncUnaryCall(nodeList));
        var grpcPeer = CreatePeer(mockClient.Object);

        var result = await grpcPeer.GetNodesAsync();
        result.ShouldBe(nodeList);
    }

    [Fact]
    public async Task RecordMetric_Test()
    {
        var block = new BlockWithTransactions
            { Header = _osTestHelper.GenerateBlock(HashHelper.ComputeFrom("PreBlockHash"), 100).Header };

        var mockClient = new Mock<PeerService.PeerServiceClient>();
        mockClient.Setup(c =>
                c.RequestBlockAsync(It.IsAny<BlockRequest>(), It.IsAny<Metadata>(), null,
                    CancellationToken.None))
            .Returns(MockAsyncUnaryCall(new BlockReply { Block = block }));

        var blockList = new BlockList();
        blockList.Blocks.Add(block);

        mockClient.Setup(c =>
                c.RequestBlocksAsync(It.IsAny<BlocksRequest>(), It.IsAny<Metadata>(), null,
                    CancellationToken.None))
            .Returns(MockAsyncUnaryCall(blockList));
        var grpcPeer = CreatePeer(mockClient.Object);

        for (var i = 0; i < 101; i++)
        {
            await grpcPeer.GetBlockByHashAsync(block.GetHash());
            await grpcPeer.GetBlocksAsync(block.Header.PreviousBlockHash, 1);
        }

        var metrics = grpcPeer.GetRequestMetrics();
        metrics["GetBlocks"].Count.ShouldBe(100);
        metrics["GetBlock"].Count.ShouldBe(100);
    }

    [Fact]
    public async Task HandleObjectDisposedException_Test()
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
    public async Task HandleRpcException_Test()
    {
        {
            var mockClient = new Mock<PeerService.PeerServiceClient>();
            mockClient.Setup(c =>
                    c.CheckHealthAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), null,
                        CancellationToken.None))
                .Throws(new AggregateException(new RpcException(new Status(StatusCode.Cancelled, ""))));
            var grpcPeer = CreatePeer(mockClient.Object);
            grpcPeer.CheckHealthAsync()
                .ShouldThrow<NetworkException>().ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
        }

        {
            var mockClient = new Mock<PeerService.PeerServiceClient>();
            mockClient.Setup(c =>
                    c.CheckHealthAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), null,
                        CancellationToken.None))
                .Throws(new AggregateException());
            var grpcPeer = CreatePeer(mockClient.Object);

            grpcPeer.CheckHealthAsync()
                .ShouldThrow<NetworkException>().ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
        }

        {
            var mockClient = new Mock<PeerService.PeerServiceClient>();
            mockClient.Setup(c =>
                    c.CheckHealthAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata>(), null,
                        CancellationToken.None))
                .Throws(new AggregateException(new RpcException(new Status(StatusCode.Cancelled, ""))));
            var grpcPeer = CreatePeer(mockClient.Object);
            await grpcPeer.DisconnectAsync(false);

            grpcPeer.CheckHealthAsync()
                .ShouldThrow<NetworkException>().ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
        }
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