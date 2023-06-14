using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Grpc;

public interface IStreamMethod
{
    MessageType Method { get; }
    Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext);
}

public abstract class StreamMethod : IStreamMethod
{
    public abstract MessageType Method { get; }
    protected readonly IGrpcRequestProcessor GrpcRequestProcessor;

    protected StreamMethod(IGrpcRequestProcessor grpcRequestProcessor)
    {
        GrpcRequestProcessor = grpcRequestProcessor;
    }

    public abstract Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext);
}

public class GetNodesMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.GetNodes;

    public GetNodesMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override async Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        return await GrpcRequestProcessor.GetNodesAsync(NodesRequest.Parser.ParseFrom(request.Message), streamContext.GetPeerInfo());
    }
}

public class HealthCheckMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.HealthCheck;

    public HealthCheckMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        return Task.FromResult(new HealthCheckReply() as IMessage);
    }
}

public class PingMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.Ping;

    public PingMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        return Task.FromResult(new PongReply() as IMessage);
    }
}

public class DisconnectMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.Disconnect;

    public DisconnectMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override async Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        await GrpcRequestProcessor.DisconnectAsync(DisconnectReason.Parser.ParseFrom(request.Message), streamContext.GetPeerInfo(), streamContext.GetPubKey(), request.RequestId);
        return new VoidReply();
    }
}

public class ConfirmHandShakeMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.ConfirmHandShake;

    public ConfirmHandShakeMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override async Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        await GrpcRequestProcessor.ConfirmHandshakeAsync(streamContext.GetPeerInfo(), streamContext.GetPubKey(), request.RequestId);
        return new VoidReply();
    }
}

public class RequestBlockMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.RequestBlock;

    public RequestBlockMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override async Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        return await GrpcRequestProcessor.GetBlockAsync(BlockRequest.Parser.ParseFrom(request.Message), streamContext.GetPeerInfo(), streamContext.GetPubKey(), request.RequestId);
    }
}

public class RequestBlocksMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.RequestBlocks;

    public RequestBlocksMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override async Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        return await GrpcRequestProcessor.GetBlocksAsync(BlocksRequest.Parser.ParseFrom(request.Message), streamContext.GetPeerInfo(), request.RequestId);
    }
}

public class BlockBroadcastMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.BlockBroadcast;

    public BlockBroadcastMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override async Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        await GrpcRequestProcessor.ProcessBlockAsync(BlockWithTransactions.Parser.ParseFrom(request.Message), streamContext.GetPubKey());
        return new VoidReply();
    }
}

public class AnnouncementBroadcastMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.AnnouncementBroadcast;

    public AnnouncementBroadcastMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override async Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        await GrpcRequestProcessor.ProcessAnnouncementAsync(BlockAnnouncement.Parser.ParseFrom(request.Message), streamContext.GetPubKey());
        return new VoidReply();
    }
}

public class TransactionBroadcastMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.TransactionBroadcast;

    public TransactionBroadcastMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override async Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        await GrpcRequestProcessor.ProcessTransactionAsync(Transaction.Parser.ParseFrom(request.Message), streamContext.GetPubKey());
        return new VoidReply();
    }
}

public class LibAnnouncementBroadcastMethod : StreamMethod, ISingletonDependency
{
    public override MessageType Method => MessageType.LibAnnouncementBroadcast;

    public LibAnnouncementBroadcastMethod(IGrpcRequestProcessor grpcRequestProcessor) : base(grpcRequestProcessor)
    {
    }

    public override async Task<IMessage> InvokeAsync(StreamMessage request, IStreamContext streamContext)
    {
        await GrpcRequestProcessor.ProcessLibAnnouncementAsync(LibAnnouncement.Parser.ParseFrom(request.Message), streamContext.GetPubKey());
        return new VoidReply();
    }
}