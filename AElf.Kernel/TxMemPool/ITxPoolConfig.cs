﻿using AElf.Kernel.Miner;
using AElf.Kernel.Types;

namespace AElf.Kernel.TxMemPool
{
    public interface ITxPoolConfig
    {
        /// <summary>
        /// pool size limit
        /// </summary>
        ulong PoolLimitSize { get; set; }
        
        /// <summary>
        /// tx size limit
        /// </summary>
        uint TxLimitSize { get; set; }
        
        /// <summary>
        /// minimal tx fee 
        /// </summary>
        ulong FeeThreshold { get; set; }

        /// <summary>
        /// chain id 
        /// </summary>
        Hash ChainId { get; set; } 

        /*
        /// <summary>
        /// minimal number of txs for entering ready list
        /// </summary>
        ulong EntryThreshold { get; }*/
        
    }
}