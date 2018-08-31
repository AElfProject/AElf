using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

namespace AElf.ChainController
{
    /// <summary>
    /// Validate the tx merkle tree root.
    /// </summary>
    public class BlockValidationFilter : IBlockValidationFilter
    {
        public Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            return Task.FromResult(block.Body.CalculateMerkleTreeRoot() != block.Header.MerkleTreeRootOfTransactions
                ? ValidationError.IncorrectTxMerkleTreeRoot
                : ValidationError.Success);
        }
    }
}