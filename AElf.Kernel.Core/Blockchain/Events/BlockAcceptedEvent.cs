namespace AElf.Kernel.Blockchain.Events
{
    public class BlockAcceptedEvent
    {
        public int ChainId { get; set; }
        public BlockHeader BlockHeader { get; set; }
    }
}