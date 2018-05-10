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
        uint TxLimitSize { get; }
        
        /// <summary>
        /// minimal tx fee 
        /// </summary>
        ulong FeeThreshold { get; }

        /// <summary>
        /// chain id 
        /// </summary>
        Hash ChainId { get;} 
        
        /// <summary>
        /// minimal number of txs for entering pool
        /// </summary>
        ulong EntryThreshold { get; }
    }
}