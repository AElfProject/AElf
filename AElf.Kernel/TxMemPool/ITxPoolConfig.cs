namespace AElf.Kernel.TxMemPool
{
    public interface ITxPoolConfig
    {
        ulong PoolLimitSize { get; }
        ulong TxLimitSize { get; }
        ulong FeeThreshold { get; }
    }
}