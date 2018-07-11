using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.Services.Miner
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
        /// represent number limit in a block
        /// </summary>
        ulong TxCount { get; set; }
    }
}