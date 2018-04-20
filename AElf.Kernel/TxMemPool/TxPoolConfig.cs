using AElf.Kernel.TxMemPool;

namespace AElf.Kernel
{
    public class TxPoolConfig : ITxPoolConfig
    {
        public static readonly TxPoolConfig Default = new TxPoolConfig()
        {
            PoolLimitSize = 10000,
            TxLimitSize = 10000,
            FeeThreshold = new Fee()
        };

        public ulong PoolLimitSize { get; set; }

        public int TxLimitSize { get; set; }

        public Fee FeeThreshold { get; set; }
    
    }
}