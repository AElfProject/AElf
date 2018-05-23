using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public interface IBlockGenerationService
    {
        /// <summary>
        /// generate block
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="txIds"></param>
        /// <returns></returns>
        Task<IBlock> BlockGeneration(Hash chainId,IEnumerable<TransactionResult> txIds);

        /// <summary>
        /// generate block header
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="merkleTreeRootForTransaction"></param>
        /// <returns></returns>
        Task<IBlockHeader> BlockHeaderGeneration(Hash chainId, Hash merkleTreeRootForTransaction);
    }
}