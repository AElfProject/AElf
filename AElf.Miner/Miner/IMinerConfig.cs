using AElf.Common;

namespace AElf.Miner.Miner
{
    public interface IMinerConfig
    {
        /// <summary>
        /// miner address
        /// </summary>
        Hash CoinBase { get; set; }
        
        /// <summary>
        /// true if parallel execution, otherwise false
        /// </summary>
        bool IsParallel { get; }
        
        
        Hash ChainId { get; set; }
        
        /// <summary>
        /// merge mining flag
        /// </summary>
        bool IsMergeMining { get; set; }
        
        /// <summary>
        /// parent miner address
        /// </summary>
        string ParentAdddress { get; set; }
        
        /// <summary>
        /// parent miner port for rpc
        /// </summary>
        string ParentPort { get; set; }
    }
}