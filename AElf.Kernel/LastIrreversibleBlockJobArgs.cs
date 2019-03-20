using AElf.Common;

namespace AElf.Kernel
{
    public class LastIrreversibleBlockJobArgs
    {
        public long LastIrreversibleBlockHeight { get; set; }
        public Hash LastIrreversibleBlockHash { get; set; }
    }
}