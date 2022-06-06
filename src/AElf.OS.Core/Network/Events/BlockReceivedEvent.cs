namespace AElf.OS.Network.Events;

public class BlockReceivedEvent
{
    public BlockReceivedEvent(BlockWithTransactions block, string senderPubkey)
    {
        BlockWithTransactions = block;
        SenderPubkey = senderPubkey;
    }

    public BlockWithTransactions BlockWithTransactions { get; }

    public string SenderPubkey { get; }
}