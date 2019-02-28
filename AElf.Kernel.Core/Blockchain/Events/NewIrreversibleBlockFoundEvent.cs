using AElf.Common;

namespace AElf.Kernel.Blockchain.Events
{
    public class NewIrreversibleBlockFoundEvent
    {
        public int ChainId { get; set; }
        public Hash BlockHash { get; set; }
        public ulong BlockHeight { get; set; }
    }
}