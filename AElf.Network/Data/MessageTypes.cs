namespace AElf.Network.Data
{
    public enum MessageTypes
    {
        AskForTx,
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