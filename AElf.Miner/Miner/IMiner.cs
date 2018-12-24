using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Miner.Miner
{
    public interface IMiner
    {
        void Init();
        
        /// <summary>
        /// This method mines a block.
        /// </summary>
        /// <returns>The block that has been produced</returns>
        Task<IBlock> Mine();
    }
}