namespace AElf.Network
{
    public enum AElfProtocolMsgType
    {
        TxRequest = 10,
        NewTransaction,
        
        HeightRequest,
        Height,
        
        Transactions,
        NewBlock,
        
        RequestBlock,
        Block,
        
        Announcement,
        HashRequest
    }
}