using AElf.Common;

namespace AElf.Kernel.Blockchain.Events
{
    public class NewIrreversibleBlockFoundEvent
    {
        public Hash BlockHash { get; set; }
        public long BlockHeight { get; set; }
    }
}