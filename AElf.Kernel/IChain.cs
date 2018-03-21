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
        Hash CurrentBlockHash { get; }

        void UpdateCurrentBlock(IBlock block);
        
        Hash Id { get; }
        
        Hash GenesisBlockHash { get; }
    }
}