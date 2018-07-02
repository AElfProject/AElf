namespace AElf.Network.Data
{
    public enum MessageTypes
    {
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