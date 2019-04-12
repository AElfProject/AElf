namespace AElf.Management.Models
{
    public class ChainStatusResult
    {
        public long LastIrreversibleBlockHeight { get; set; }
        
        public long LongestChainHeight { get; set; }
        
        public long BestChainHeight { get; set; }
    }
}