using AElf.Kernel.TxMemPool;

namespace AElf.Kernel
{
    public class TxPoolConfig : ITxPoolConfig
    {
        
        public TxPoolConfig(ulong poolLimitSize, Fee feeThreshold, ulong txLimitSize)
        {
            PoolLimitSize = poolLimitSize;
            FeeThreshold = feeThreshold;
            TxLimitSize = txLimitSize;
        }

        public ulong PoolLimitSize { get; }

        public ulong TxLimitSize { get; }

        public Fee FeeThreshold { get; }
    }
}