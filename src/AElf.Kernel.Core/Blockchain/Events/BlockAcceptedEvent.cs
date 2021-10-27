namespace AElf.Kernel.Blockchain.Events
{
    public class BlockAcceptedEvent
    {
        public BlockExecutedSet BlockExecutedSet { get; set; }

        public Block Block => BlockExecutedSet.Block;
    }
}