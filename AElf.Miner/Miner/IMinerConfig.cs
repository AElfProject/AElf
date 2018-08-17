using AElf.Kernel;

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
    }
}