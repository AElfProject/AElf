using AElf.Cryptography.ECDSA;
using AElf.Common;

namespace AElf.ChainController.TxMemPool
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
        
        /// <summary>
        /// represent miner self
        /// </summary>
        ECKeyPair EcKeyPair { get; set; }

        /// <summary>
        /// the minimal number of tx in one block
        /// </summary>
        int Minimal { get; set; }
        
        /// <summary>
        /// the Maximal number of tx in one block
        /// </summary>
        int Maximal { get; set; }
    }
}