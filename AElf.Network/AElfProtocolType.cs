namespace AElf.Network
{
    public enum AElfProtocolType
    {
        TxRequest = 10,
        Tx,
        
        HeightRequest,
        Height,
        
        BroadcastTx,
        BroadcastBlock,
        
        RequestBlock,
        Block
    }
}