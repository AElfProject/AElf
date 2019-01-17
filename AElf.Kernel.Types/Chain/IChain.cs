using AElf.Common;

namespace AElf.Kernel
{
    /// <summary>
    /// A chain only provides the ability to add and load block, but not keep them in memory
    /// </summary>
    public interface IChain 
    {
        long Id { get; }
        
        Hash GenesisBlockHash { get; }
    }
}