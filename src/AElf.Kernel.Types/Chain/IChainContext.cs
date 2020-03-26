using AElf.Types;

namespace AElf.Kernel
{
    /// <summary>
    /// a running chain context
    /// </summary>
    public interface IChainContext : IBlockIndex
    {
        long BlockHeight { get; set; }
        Hash BlockHash { get; set; }
        IStateCache StateCache { get; set; }
    }
}