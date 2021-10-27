using AElf.Types;

namespace AElf.Kernel
{
    /// <summary>
    /// a running chain context
    /// </summary>
    public interface IChainContext : IBlockIndex
    {
        IStateCache StateCache { get; set; }
    }
}