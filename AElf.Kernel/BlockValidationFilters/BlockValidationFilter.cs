using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Services;

namespace AElf.Kernel.BlockValidationFilters
{
    public class BlockValidationFilter : IBlockValidationFilter
    {
        public Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            if (block.Body.CalculateMerkleTreeRoot() != block.Header.MerkleTreeRootOfTransactions)
                return Task.FromResult(ValidationError.InvalidBlock);
            return Task.FromResult(ValidationError.Success);
        }
    }
}