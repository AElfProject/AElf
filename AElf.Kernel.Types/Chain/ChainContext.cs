namespace AElf.Kernel
{
    public class ChainContext : IChainContext
    {
        public Hash ChainId { get; set; }
        public ulong BlockHeight { get; set; }
        public Hash BlockHash { get; set; }
    }
}