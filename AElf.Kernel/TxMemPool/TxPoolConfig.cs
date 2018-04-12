using AElf.Kernel.TxMemPool;

namespace AElf.Kernel
{
    public class TxPoolConfig : ITxPoolConfig
    {
        public TxPoolConfig(ulong poolLimitSize, int txLimitSize)
        {
            PoolLimitSize = poolLimitSize;
            TxLimitSize = txLimitSize;
        }

        public ulong PoolLimitSize { get; }
        public int TxLimitSize { get; }
    }
}