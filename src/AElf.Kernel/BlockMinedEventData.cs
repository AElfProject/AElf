namespace AElf.Kernel
{
    public class BlockMinedEventData
    {
        public BlockHeader BlockHeader { get; set; }
        
        public bool HasFork { get; set; }
    }
}