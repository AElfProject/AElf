namespace AElf.OS.Network.Grpc
{
    public interface IPeerAuthentificator
    {
        bool AuthenticatePeer(string peer, Handshake handshake);
        bool IsAuthenticated(string peer);
        bool FinalizeAuth(GrpcPeer peer);
        Handshake GetHandshake();
    }
}