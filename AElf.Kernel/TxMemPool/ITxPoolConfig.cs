namespace AElf.Kernel.TxMemPool
{
    public interface ITxPoolConfig
    {
        /// <summary>
        /// pool size limit
        /// </summary>
        ulong PoolLimitSize { get; }
        
        /// <summary>
        /// tx size limit
        /// </summary>
        ulong TxLimitSize { get; }
        
        /// <summary>
        /// minimal tx fee 
        /// </summary>
        ulong FeeThreshold { get; }
    }
}