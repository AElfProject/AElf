using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// Use the received transactions produce a block
    /// </summary>
    public interface IBlockProducer
    {
        Task<IBlock> CreateBlockAsync();
    }
}