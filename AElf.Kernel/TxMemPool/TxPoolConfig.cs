using AElf.Kernel.Miner;

namespace AElf.Kernel.TxMemPool
{
    public class TxPoolConfig : ITxPoolConfig
    {
        public static readonly TxPoolConfig Default = new TxPoolConfig
        {
            PoolLimitSize = 1024 * 1024,
            TxLimitSize = 1024 * 1,
            EntryThreshold = 5,
            ChainId = Hash.Generate()
        };
        
        public Hash ChainId { get; set; }

        public ulong PoolLimitSize { get; set; }

        public uint TxLimitSize { get; set; }

        public ulong FeeThreshold { get; set; }
       
        public ulong EntryThreshold { get; set; }
    
    }
}