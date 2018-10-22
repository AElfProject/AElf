using System.Threading.Tasks;
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
            {
                return Task.FromResult(BlockValidationResult.BlockIsNull);
            }

            if (block.Body.TransactionsCount == 0)
            {
                return Task.FromResult(BlockValidationResult.NoTransaction);
            }

            if (block.Body.CalculateMerkleTreeRoots() != block.Header.MerkleTreeRootOfTransactions)
            {
                return Task.FromResult(BlockValidationResult.IncorrectTxMerkleTreeRoot);
            }

            if (block.Body.SideChainTransactionsRoot != block.Header.SideChainTransactionsRoot
                || block.Body.SideChainBlockHeadersRoot != block.Header.SideChainBlockHeadersRoot)
            {
                return Task.FromResult(BlockValidationResult.IncorrectSideChainInfo);
            }

            return Task.FromResult(BlockValidationResult.Success);
        }
    }
}