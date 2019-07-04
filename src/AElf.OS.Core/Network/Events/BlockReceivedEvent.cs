using AElf.Kernel;

namespace AElf.OS.Network.Events
{
    public class BlockReceivedEvent
    {
        public BlockWithTransactions BlockWithTransactions { get; }

        public BlockReceivedEvent(BlockWithTransactions block)
        {
            BlockWithTransactions = block;
        }
    }
}