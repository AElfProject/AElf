using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.ChainController.Application.Filters
{
    /// <summary>
    /// Some basic validations:
    /// Validate the tx merkle tree root.
    /// </summary>
    public class BlockValidationFilter : IBlockValidationFilter , ITransientDependency
    {
        public Task<BlockValidationResult> ValidateBlockAsync(IBlock block)
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
            return Task.FromResult(BlockValidationResult.Success);
        }
    }
}