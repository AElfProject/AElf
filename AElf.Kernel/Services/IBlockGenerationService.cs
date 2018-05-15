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
        /// <param name="lastBlockHash"></param>
        /// <param name="txIds"></param>
        /// <returns></returns>
        Task<Block> BlockGeneration(Hash chainId, Hash lastBlockHash, IEnumerable<TransactionResult> txIds);

        /// <summary>
        /// generate block header
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="lastBlockHash"></param>
        /// <param name="merkleTreeRootForTransaction"></param>
        /// <returns></returns>
        Task<BlockHeader> BlockHeaderGeneration(Hash chainId, Hash lastBlockHash, Hash merkleTreeRootForTransaction);
    }
}