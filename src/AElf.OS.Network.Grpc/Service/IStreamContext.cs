using System.Text;
using Google.Protobuf.Collections;
using Grpc.Core;

namespace AElf.OS.Network.Grpc;

public interface IStreamContext
{
    string GetPeerInfo();
    string GetPubKey();
    string GetSessionId();
    void SetPeerInfo(string peerInfo);
}

public class ServiceStreamContext : IStreamContext
{
    public ServerCallContext Context;

    public ServiceStreamContext(ServerCallContext context)
    {
        Context = context;
    }

    public string GetPeerInfo()
    {
        return Context.GetPeerInfo();
    }

    public string GetPubKey()
    {
        return Context.GetPublicKey();
    }

    public string GetSessionId()
    {
        return Context.GetSessionId()?.ToHex();
    }

    public void SetPeerInfo(string peerInfo)
    {
        Context.RequestHeaders.Add(new Metadata.Entry(GrpcConstants.PeerInfoMetadataKey, peerInfo));
    }
}

public class StreamMessageMetaStreamContext : IStreamContext
{
    private MapField<string, string> _meta;

    public StreamMessageMetaStreamContext(MapField<string, string> meta)
    {
        _meta = meta;
    }

    public string GetPeerInfo()
    {
        return _meta[GrpcConstants.PeerInfoMetadataKey];
    }

    public string GetPubKey()
    {
        return _meta[GrpcConstants.PubkeyMetadataKey];
    }

    public string GetSessionId()
    {
        return _meta[GrpcConstants.SessionIdMetadataKey];
    }

    public void SetPeerInfo(string peerInfo)
    {
        _meta[GrpcConstants.PeerInfoMetadataKey] = peerInfo;
    }
}