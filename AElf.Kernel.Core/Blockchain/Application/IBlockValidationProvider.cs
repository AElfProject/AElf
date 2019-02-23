using System.Threading.Tasks;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockValidationProvider
    {
        Task<bool> ValidateBlockBeforeExecuteAsync(int chainId, IBlock block);

        Task<bool> ValidateBlockAfterExecuteAsync(int chainId, IBlock block);
    }

    public class BlockValidationProvider : IBlockValidationProvider
    {
        public async Task<bool> ValidateBlockBeforeExecuteAsync(int chainId, IBlock block)
        {
            if (block?.Header == null || block.Body == null)
            {
                return false;
            }

            if (block.Body.TransactionsCount == 0)
            {
                return false;
            }

            if (block.Body.CalculateMerkleTreeRoots() != block.Header.MerkleTreeRootOfTransactions)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(int chainId, IBlock block)
        {
            return true;
        }
    }
}