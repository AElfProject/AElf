namespace AElf.Kernel
{
    public class MergeBlockStateJobArgs
    {
        public string LastIrreversibleBlockHash { get; set; }
        public long LastIrreversibleBlockHeight { get; set; }
    }
}