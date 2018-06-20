namespace AElf.Network.Data
{
    public enum MessageTypes
    {
        AskForTx,
        SendTx,
        
        RequestPeers,
        ReturnPeers,
        
        BroadcastTx,
        BroadcastBlock,
        
        RequestBlock,
        SendBlock
    }
}