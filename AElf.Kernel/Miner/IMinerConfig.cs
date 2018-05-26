namespace AElf.Kernel.Miner
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
        bool IsParallel { get; set; }
        
        
        Hash ChainId { get; set; }
        /// <summary>
        /// represent number limit in a block
        /// </summary>
        ulong TxCountLimit { get; }
    }
}