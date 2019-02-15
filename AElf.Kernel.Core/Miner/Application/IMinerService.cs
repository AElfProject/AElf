using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Miner.Application
{
    public interface IMinerService
    {
        /// <summary>
        /// This method mines a block.
        /// </summary>
        /// <returns>The block that has been produced</returns>
        Task<IBlock> MineAsync(int chainId, Hash previousBlockHash, ulong previousBlockHeight);
    }
}