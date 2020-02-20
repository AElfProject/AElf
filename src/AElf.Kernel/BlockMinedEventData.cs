namespace AElf.Kernel
{
    public class BlockMinedEventData
    {
        public BlockHeader BlockHeader { get; set; }
        
        //TODO!! not used
        public bool HasFork { get; set; }
    }
}