using AElf.Kernel;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Protocol.Types;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network.Grpc;

public static class GrpcTestPeerHelper
{
    private static Channel CreateMockChannel()
    {
        return new("127.0.0.1:9999", ChannelCredentials.Insecure);
    }

    public static GrpcPeer CreateBasicPeer(string ip, string pubkey)
    {
        return CreatePeerWithInfo(ip,
            new PeerConnectionInfo
                { Pubkey = pubkey, SessionId = new byte[] { 0, 1, 2 }, ConnectionTime = TimestampHelper.GetUtcNow() });
    }

    public static GrpcPeer CreatePeerWithInfo(string ip, PeerConnectionInfo info)
    {
        AElfPeerEndpointHelper.TryParse(ip, out var endpoint);
        var peer = new GrpcPeer(new GrpcClient(CreateMockChannel(), null), endpoint, info);
        peer.InboundSessionId = new byte[] { 0, 1, 2 };
        return peer;
    }

    public static GrpcPeer CreatePeerWithClient(string ip, string pubkey, PeerService.PeerServiceClient client)
    {
        AElfPeerEndpointHelper.TryParse(ip, out var endpoint);
        var peer = new GrpcPeer(new GrpcClient(CreateMockChannel(), client), endpoint,
            new PeerConnectionInfo { Pubkey = pubkey, SessionId = new byte[] { 0, 1, 2 } });
        peer.InboundSessionId = new byte[] { 0, 1, 2 };
        return peer;
    }

    public static GrpcPeer CreateNewPeer(string ipAddress = "127.0.0.1:2000", bool isValid = true,
        string publicKey = null)
    {
        var pubkey = publicKey ?? NetworkTestConstants.FakePubkey;
        var channel = new Channel(ipAddress, ChannelCredentials.Insecure);

        PeerService.PeerServiceClient client;

        if (isValid)
            client = new PeerService.PeerServiceClient(channel.Intercept(metadata =>
            {
                metadata.Add(GrpcConstants.PubkeyMetadataKey, pubkey);
                return metadata;
            }));
        else
            client = new PeerService.PeerServiceClient(channel);

        var connectionInfo = new PeerConnectionInfo
        {
            Pubkey = pubkey,
            ProtocolVersion = KernelConstants.ProtocolVersion,
            ConnectionTime = TimestampHelper.GetUtcNow(),
            SessionId = new byte[] { 0, 1, 2 },
            IsInbound = true
        };

        AElfPeerEndpointHelper.TryParse(ipAddress, out var endpoint);
        var peer = new GrpcPeer(new GrpcClient(channel, client), endpoint, connectionInfo);
        peer.InboundSessionId = new byte[] { 0, 1, 2 };

        return peer;
    }
}