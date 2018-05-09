namespace AElf.Kernel.TxMemPool
{
    public class TxPoolConfig : ITxPoolConfig
    {
        public static readonly TxPoolConfig Default = new TxPoolConfig
        {
            PoolLimitSize = 10000,
            TxLimitSize = 10000,
            EntryThreshold = 5
        };

        public ulong PoolLimitSize { get; set; }

        public int TxLimitSize { get; set; }

        public ulong FeeThreshold { get; set; }
       
        public ulong EntryThreshold { get; set; }
    }
}