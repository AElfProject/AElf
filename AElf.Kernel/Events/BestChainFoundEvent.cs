using AElf.Common;

namespace AElf.Kernel.Events
{
    public class BestChainFoundEvent
    {
        public int ChainId { get; set; }
        public Hash BlockHash { get; set; }
        public ulong BlockHeight { get; set; }
    }
}