using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.ChainController
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