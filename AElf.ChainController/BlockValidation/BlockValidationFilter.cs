using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    /// <summary>
    /// Validate the tx merkle tree root.
    /// </summary>
    public class BlockValidationFilter : IBlockValidationFilter
    {
        public Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            if (block?.Header == null || block.Body == null)
                return Task.FromResult(ValidationError.InvalidBlock);
            
            ValidationError res = ValidationError.Success;
            if(block.Body.CalculateMerkleTreeRoots() != block.Header.MerkleTreeRootOfTransactions)
                res = ValidationError.IncorrectTxMerkleTreeRoot;
            else if (block.Body.SideChainTransactionsRoot != block.Header.SideChainTransactionsRoot
                     || block.Body.SideChainBlockHeadersRoot != block.Header.SideChainBlockHeadersRoot)
                res = ValidationError.IncorrectSideChainInfo;
            
            return Task.FromResult(res);
        }
    }
}