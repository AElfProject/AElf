using System.Threading.Tasks;
using AElf.OS.Network.Grpc.Helpers;
using AElf.Types;
using Google.Protobuf;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public class StreamClient
{
    private const int StreamWaitTime = 500;
    private readonly IAsyncStreamWriter<StreamMessage> _clientStreamWriter;
    private readonly IStreamTaskResourcePool _streamTaskResourcePool;

    public StreamClient(IAsyncStreamWriter<StreamMessage> clientStreamWriter, IStreamTaskResourcePool streamTaskResourcePool)
    {
        _clientStreamWriter = clientStreamWriter;
        _streamTaskResourcePool = streamTaskResourcePool;
    }

    public async Task<NodeList> GetNodesAsync(NodesRequest nodesRequest, Metadata header, GrpcRequest request)
    {
        var reply = await RequestAsync(StreamType.GetNodes, nodesRequest.ToByteString(), header, StreamType.GetNodesReply, GetTimeOutFromHeader(header));
        var nodeList = new NodeList();
        nodeList.MergeFrom(reply.Body);
        return nodeList;
    }

    public async Task HandleShake(HandshakeRequest request, Metadata header)
    {
        await RequestAsync(StreamType.HandShake, request.ToByteString(), header, StreamType.HandShakeReply);
    }

    public async Task CheckHealthAsync(Metadata header, GrpcRequest request)
    {
        await RequestAsync(StreamType.HealthCheck, null, header, StreamType.HealthCheckReply, GetTimeOutFromHeader(header));
    }

    public async Task<BlockWithTransactions> RequestBlockAsync(BlockRequest blockRequest, Metadata header, GrpcRequest request)
    {
        var reply = await RequestAsync(StreamType.RequestBlock, blockRequest.ToByteString(), header, StreamType.RequestBlockReply, GetTimeOutFromHeader(header));
        var blockWithTransactions = new BlockWithTransactions();
        blockWithTransactions.MergeFrom(reply.Body);
        return blockWithTransactions;
    }

    public async Task<BlockList> RequestBlocksAsync(BlocksRequest blockRequest, Metadata header, GrpcRequest request)
    {
        var reply = await RequestAsync(StreamType.RequestBlocks, blockRequest.ToByteString(), header, StreamType.RequestBlocksReply, GetTimeOutFromHeader(header));
        var blockList = new BlockList();
        blockList.MergeFrom(reply.Body);
        return blockList;
    }

    public async Task DisconnectAsync(DisconnectReason disconnectReason, Metadata header, GrpcRequest request)
    {
        await RequestAsync(StreamType.Disconnect, disconnectReason.ToByteString(), header, StreamType.DisconnectReply);
    }

    public async Task ConfirmHandshakeAsync(ConfirmHandshakeRequest confirmHandshakeRequest, Metadata header, GrpcRequest request)
    {
        await RequestAsync(StreamType.HandShake, confirmHandshakeRequest.ToByteString(), header, StreamType.HandShakeReply, GetTimeOutFromHeader(header));
    }

    public async Task BroadcastBlockAsync(BlockWithTransactions blockWithTransactions, Metadata header)
    {
        await RequestAsync(StreamType.BlockBroadcast, blockWithTransactions.ToByteString(), header, StreamType.BlockBroadcastReply);
    }

    public async Task<PongReply> Ping(Metadata header)
    {
        var msg = await RequestAsync(StreamType.Ping, null, header, StreamType.PongReply);
        return msg == null ? null : PongReply.Parser.ParseFrom(msg.Body);
    }

    public async Task BroadcastAnnouncementBlockAsync(BlockAnnouncement header, Metadata meta)
    {
        await RequestAsync(StreamType.AnnouncementBroadcast, header.ToByteString(), meta, StreamType.AnnouncementBroadcastReply);
    }

    public async Task BroadcastTransactionAsync(Transaction transaction, Metadata meta)
    {
        await RequestAsync(StreamType.TransactionBroadcast, transaction.ToByteString(), meta, StreamType.TransactionBroadcastReply);
    }

    public async Task BroadcastLibAnnouncementAsync(LibAnnouncement libAnnouncement, Metadata header)
    {
        await RequestAsync(StreamType.LibAnnouncementBroadcast, libAnnouncement.ToByteString(), header, StreamType.LibAnnouncementBroadcastReply);
    }

    private int GetTimeOutFromHeader(Metadata header)
    {
        var t = header.Get(GrpcConstants.TimeoutMetadataKey)?.Value;
        return int.Parse(t);
    }

    private async Task<StreamMessage> RequestAsync(StreamType replyType, ByteString reply, Metadata header, StreamType expectReturnType, int timeout = StreamWaitTime)
    {
        var requestId = CommonHelper.GenerateRequestId();
        var streamMessage = new StreamMessage { StreamType = replyType, RequestId = requestId, Body = reply };
        AddAllHeaders(streamMessage, header);
        await _clientStreamWriter.WriteAsync(streamMessage);
        _streamTaskResourcePool.RegistryTaskPromise(requestId, expectReturnType, new TaskCompletionSource<StreamMessage>());
        return await _streamTaskResourcePool.GetResult(requestId, timeout);
    }

    private void AddAllHeaders(StreamMessage streamMessage, Metadata header)
    {
        if (header == null)
        {
            return;
        }

        foreach (var e in header)
        {
            streamMessage.Meta.Add(e.Key, e.Value);
        }
    }
}