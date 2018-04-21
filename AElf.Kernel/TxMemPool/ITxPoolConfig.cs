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
        int TxLimitSize { get; }
        
        /// <summary>
        /// minimal tx fee 
        /// </summary>
        Fee FeeThreshold { get; }
        
        /// <summary>
        /// minimal number of txs for entering pool
        /// </summary>
        int EntryThreshold { get; }
    }
}