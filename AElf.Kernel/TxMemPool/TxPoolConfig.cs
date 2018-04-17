using AElf.Kernel.TxMemPool;

namespace AElf.Kernel
{
    public class TxPoolConfig : ITxPoolConfig
    {
        
        public TxPoolConfig(ulong poolLimitSize, Fee feeThreshold, int txLimitSize)
        {
            PoolLimitSize = poolLimitSize;
            FeeThreshold = feeThreshold;
            TxLimitSize = txLimitSize;
        }

        public ulong PoolLimitSize { get; }

        public int TxLimitSize { get; }

        public Fee FeeThreshold { get; }
    }
}