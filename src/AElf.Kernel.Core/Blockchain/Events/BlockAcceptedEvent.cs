namespace AElf.Kernel.Blockchain.Events
{
    public class BlockAcceptedEvent
    {
        public Block Block { get; set; }
        
        public bool HasFork { get; set; }
    }
}