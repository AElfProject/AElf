namespace AElf.Kernel.TxMemPool
{
    public interface ITxPoolConfig
    {
        ulong PoolLimitSize { get; }
        int TxLimitSize { get; }
    }
}