namespace AElf.Kernel.TxMemPool
{
    public class TxPoolConfig : ITxPoolConfig
    {
        public static readonly TxPoolConfig Default = new TxPoolConfig
        {
            PoolLimitSize = 1024 * 1024,
            TxLimitSize = 1024 * 1,
            EntryThreshold = 5
        };

        public ulong PoolLimitSize { get; set; }

        public uint TxLimitSize { get; set; }

        public ulong FeeThreshold { get; set; }
       
        public ulong EntryThreshold { get; set; }
    }
}