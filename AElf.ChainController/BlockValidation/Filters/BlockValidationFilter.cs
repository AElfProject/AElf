using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    /// <summary>
    /// Some basic validations:
    /// Validate the tx merkle tree root.
    /// </summary>
    public class BlockValidationFilter : IBlockValidationFilter
    {
        public Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context)
        {
            if (block?.Header == null || block.Body == null)
                return Task.FromResult(BlockValidationResult.InvalidBlock);
            
            var res = BlockValidationResult.Success;
            if(block.Body.CalculateMerkleTreeRoots() != block.Header.MerkleTreeRootOfTransactions)
                res = BlockValidationResult.IncorrectTxMerkleTreeRoot;
            else if (block.Body.SideChainTransactionsRoot != block.Header.SideChainTransactionsRoot
                     || block.Body.SideChainBlockHeadersRoot != block.Header.SideChainBlockHeadersRoot)
                res = BlockValidationResult.IncorrectSideChainInfo;
            
            return Task.FromResult(res);
        }
    }
}