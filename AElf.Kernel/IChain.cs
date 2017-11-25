using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// A chain only provides the ability to add and load block, but not keep them in memory
    /// </summary>
    public interface IChain
    {
        Task AddBlockAsync(IBlock block);
        Task GetBlock(IHash hash);
    }
}