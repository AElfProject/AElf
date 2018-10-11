using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public class BlockValidationService: IBlockValidationService
    {
        private readonly IEnumerable<IBlockValidationFilter> _filters;

        public BlockValidationService(IEnumerable<IBlockValidationFilter> filters)
        {
            _filters = filters;
        }

        public async Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            int error = (int) BlockValidationResult.Success; 
            foreach (var filter in _filters)
            {
                error = Math.Max((int)await filter.ValidateBlockAsync(block, context, keyPair), error);
                if (error == 3)
                    return BlockValidationResult.InvalidBlock;
            }
            
            return (BlockValidationResult) error;
        }
    }

    public interface IBlockValidationFilter
    {
        Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair);
    }
}