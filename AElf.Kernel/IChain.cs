namespace AElf.Kernel
{
    /// <summary>
    /// A chain only provides the ability to add and load block, but not keep them in memory
    /// </summary>
    public interface IChain
    {
        /// <summary>
        /// Current block height
        /// </summary>
        long CurrentBlockHeight { get; }
        
        /// <summary>
        /// Current block hash
        /// </summary>
        IHash<IBlock> CurrentBlockHash { get; }

        void UpdateCurrentBlock(IBlock block);
        
        IHash<string> Id { get; }
        
        IHash<IBlock> GenesisBlockHash { get; }
    }

    public interface IChainBlock
    {
        IBlock Block { get; set; }
        IHash<string> ChainId { get; set; }
        long Height { get; set; }
    }
}