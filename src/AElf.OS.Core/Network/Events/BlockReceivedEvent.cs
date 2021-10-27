namespace AElf.OS.Network.Events
{
    public class BlockReceivedEvent
    {
        public BlockWithTransactions BlockWithTransactions { get; }
        
        public string SenderPubkey { get; }

        public BlockReceivedEvent(BlockWithTransactions block, string senderPubkey)
        {
            BlockWithTransactions = block;
            SenderPubkey = senderPubkey;
        }
    }
}