using System.Threading.Tasks;

namespace AElf.Kernel.Miner
{
    public interface IMinerService
    {
        /// <summary>
        /// This method mines a block.
        /// </summary>
        /// <returns>The block that has been produced</returns>
        Task<IBlock> Mine();
    }
}