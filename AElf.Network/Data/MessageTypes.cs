namespace AElf.Network.Data
{
    public enum MessageTypes
    {
        AskForTx,
        
        RequestPeers,
        ReturnPeers,
        
        BroadcastTx,
        BroadcastBlock,
        
        RequestBlock,
        SendBlock
    }
}