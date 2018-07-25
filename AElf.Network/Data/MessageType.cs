namespace AElf.Network.Data
{
    public enum MessageType
    {
        Auth,
        Ping,
        Pong,
        Disconnect,
        
        TxRequest,
        Tx,
        
        HeightRequest,
        Height,
        
        RequestPeers,
        Peers,
        
        BroadcastTx,
        BroadcastBlock,
        
        RequestBlock,
        Block
    }
}