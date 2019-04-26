namespace AElf.Kernel
{
    // TODO: rename class name
    public class ChainContext : IChainContext
    {
        public long BlockHeight { get; set; }
        public Hash BlockHash { get; set; }
        public IStateCache StateCache { get; set; }
    }
}