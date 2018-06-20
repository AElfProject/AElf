namespace AElf.Network.Data
{
    public enum MessageTypes
    {
        AskForTx,
        SendTx,
        
        HeightRequest,
        Height,
        
        RequestPeers,
        ReturnPeers,
        
        BroadcastTx,
        BroadcastBlock,
        
        RequestBlock,
        SendBlock
    }
}