namespace AElf.Network.Data
{
    public enum MessageType
    {
        Auth = 0,
        Ping,
        Pong,
        RequestPeers,
        Peers
    }
}