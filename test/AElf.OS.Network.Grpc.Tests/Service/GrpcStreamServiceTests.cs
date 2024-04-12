using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc.Helpers;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Protocol;
using AElf.OS.Network.Protocol.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Grpc.Core;
using Grpc.Core.Testing;
using Grpc.Core.Utils;
using Moq;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Grpc;

public class GrpcStreamServiceTests : GrpcNetworkWithChainTestBase
{
    private readonly OSTestHelper _osTestHelper;
    private readonly IStreamService _streamService;
    private readonly IPeerPool _peerPool;

    public GrpcStreamServiceTests()
    {
        _osTestHelper = GetRequiredService<OSTestHelper>();
        _streamService = GetRequiredService<IStreamService>();
        _peerPool = GetRequiredService<IPeerPool>();
    }

    private ServerCallContext BuildServerCallContext(Metadata metadata = null, string address = null)
    {
        return TestServerCallContext.Create("mock", null, TimestampHelper.GetUtcNow().AddHours(1).ToDateTime(),
            metadata ?? new Metadata(), CancellationToken.None,
            address ?? "ipv4:127.0.0.1:5555", null, null, m => TaskUtils.CompletedTask, () => new WriteOptions(),
            writeOptions => { });
    }

    [Fact]
    public void StreamMessageMetaStreamContextTest()
    {
        var context = new StreamMessageMetaStreamContext(new MapField<string, string>()
        {
            { GrpcConstants.PubkeyMetadataKey, "111" },
            { GrpcConstants.SessionIdMetadataKey, "222" },
            { GrpcConstants.PeerInfoMetadataKey, "333" },
        });
        Assert.True(context.GetPeerInfo().Equals("333"));
        Assert.True(context.GetPubKey().Equals("111"));
        Assert.True(context.GetSessionId().Equals("222"));
        context.SetPeerInfo("123");
        Assert.True(context.GetPeerInfo().Equals("123"));

        var context1 = new ServiceStreamContext(BuildServerCallContext());
        Assert.True(context1.GetPeerInfo() == null);
        Assert.True(context1.GetPubKey() == null);
        Assert.True(context1.GetSessionId() == null);
        context1.SetPeerInfo("123");
        Assert.True(context1.GetPeerInfo().Equals("123"));
    }

    [Fact]
    public async Task StreamTaskResourcePoolTest()
    {
        var pool = new StreamTaskResourcePool();
        var requestId = "23";
        var p = new TaskCompletionSource<StreamMessage>();
        pool.RegistryTaskPromiseAsync(requestId, MessageType.Ping, p);
        pool.TrySetResult(requestId, new StreamMessage { Message = new PongReply().ToByteString(), MessageType = MessageType.Ping, RequestId = requestId, StreamType = StreamType.Reply });
        var res = await pool.GetResultAsync(p, requestId, 100);
        res.Item1.ShouldBe(true);
    }

    [Fact]
    public void RequestIdTest()
    {
        var requestId = CommonHelper.GenerateRequestId();
        Task.Delay(5);
        var l = CommonHelper.GetRequestLatency(requestId);
        Assert.True(l >= 0);
    }

    [Fact]
    public async Task GrpcStreamPeerTest()
    {
        var mockClient = new Mock<PeerService.PeerServiceClient>();
        var grpcClient = new GrpcClient(new("127.0.0.1:9999", ChannelCredentials.Insecure), mockClient.Object);
        var peer = new GrpcStreamPeer(grpcClient, null, new PeerConnectionInfo() { Pubkey = "0471b4ea88d8cf3d4c58c12e1306a4fdfde64b3a511e8b023c0412032869f47e1ddeaa9b82318a7d1b8d08eb484cfe93a51c319750b65e3df8ffd822a446251b64" }, null, null,
            new StreamTaskResourcePool(), new Dictionary<string, string>() { { "tmp", "value" } });
        Assert.Throws<NullReferenceException>(peer.BuildCall);

        peer.StartServe(null);
        peer.SetStreamSendCallBack(null);
        peer.DisposeAsync().ShouldThrow<NullReferenceException>();
        peer.DisconnectAsync(true).ShouldThrow<ArgumentNullException>();

        var block = new BlockWithTransactions
            { Header = _osTestHelper.GenerateBlock(HashHelper.ComputeFrom("PreBlockHash"), 100).Header };
        peer.GetBlocksAsync(block.GetHash(), 10).ShouldThrow<ArgumentNullException>();
        peer.HandShakeAsync(new HandshakeRequest()).ShouldThrow<NullReferenceException>();
        peer.ConfirmHandshakeAsync().ShouldThrow<NullReferenceException>();
        peer.GetBlockByHashAsync(block.GetHash()).ShouldThrow<NullReferenceException>();
        peer.GetNodesAsync().ShouldThrow<NullReferenceException>();
        peer.CheckHealthAsync().ShouldThrow<ArgumentNullException>();
        peer.HandleRpcException(new RpcException(Status.DefaultSuccess), "");

        var nodePubkey = "0471b4ea88d8cf3d4c58c12e1306a4fdfde64b3a511e8b023c0412032869f47e1ddeaa9b82318a7d1b8d08eb484cfe93a51c319750b65e3df8ffd822a446251b64";
        var peerback = new GrpcStreamBackPeer(null, new PeerConnectionInfo() { Pubkey = nodePubkey }, null,
            new StreamTaskResourcePool(), new Dictionary<string, string>() { { GrpcConstants.PubkeyMetadataKey, nodePubkey } });
        peerback.ConnectionStatus.ShouldBe("Stream Closed");
        var res = await peerback.TryRecoverAsync();
        res.ShouldBe(false);
        var re = peerback.HandleRpcException(new RpcException(new Status(StatusCode.Cancelled, "")), "");
        re.ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
        re = peerback.HandleRpcException(new RpcException(new Status(StatusCode.Unknown, "")), "");
        re.ExceptionType.ShouldBe(NetworkExceptionType.Unrecoverable);
    }

    [Fact]
    public async Task StreamServiceTests()
    {
        var mockClient = new Mock<PeerService.PeerServiceClient>();
        var grpcClient = new GrpcClient(new("127.0.0.1:9999", ChannelCredentials.Insecure), mockClient.Object);
        var nodePubkey = "0471b4ea88d8cf3d4c58c12e1306a4fdfde64b3a511e8b023c0412032869f47e1ddeaa9b82318a7d1b8d08eb484cfe93a51c319750b65e3df8ffd822a446251b64";
        var peer = new GrpcStreamPeer(grpcClient, null, new PeerConnectionInfo() { Pubkey = "0471b4ea88d8cf3d4c58c12e1306a4fdfde64b3a511e8b023c0412032869f47e1ddeaa9b82318a7d1b8d08eb484cfe93a51c319750b65e3df8ffd822a446251b64" }, null, null,
            new StreamTaskResourcePool(), new Dictionary<string, string>() { { GrpcConstants.PubkeyMetadataKey, nodePubkey } });
        peer.InboundSessionId = "123".GetBytes();
        _peerPool.TryAddPeer(peer);
        var sessionId = HandshakeProvider.GenerateRandomToken();
        _streamService.ProcessStreamRequestAsync(new StreamMessage { StreamType = StreamType.Reply, MessageType = MessageType.HealthCheck, RequestId = "123", Message = new HealthCheckRequest().ToByteString() },
            BuildServerCallContext(new Metadata() { { GrpcConstants.SessionIdMetadataKey, sessionId }, }));
        _streamService.ProcessStreamRequestAsync(new StreamMessage { StreamType = StreamType.Request, MessageType = MessageType.HealthCheck, RequestId = "123", Message = new HealthCheckRequest().ToByteString() },
            BuildServerCallContext(new Metadata() { { GrpcConstants.SessionIdMetadataKey, sessionId }, }));

        _streamService.ProcessStreamPeerException(new NetworkException("", NetworkExceptionType.Unrecoverable), peer).ShouldThrow<NullReferenceException>();
        await _streamService.ProcessStreamPeerException(new NetworkException("", NetworkExceptionType.PeerUnstable), peer);
    }
}