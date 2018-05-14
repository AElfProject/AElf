namespace AElf.Kernel
{
    /// <summary>
    /// A chain only provides the ability to add and load block, but not keep them in memory
    /// </summary>
    public interface IChain : ISerializable
    {
        /// <summary>
        /// Current block height
        /// </summary>
        ulong CurrentBlockHeight { get; }
        
        /// <summary>
        /// Current block hash
        /// </summary>
        Hash CurrentBlockHash { get; set; }

        void UpdateCurrentBlock(Block block);
        
        Hash Id { get; }
        
        Hash GenesisBlockHash { get; }
    }
}