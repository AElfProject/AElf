using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.ChainController
{
    public class BlockVaildationService: IBlockVaildationService
    {
        private readonly IEnumerable<IBlockValidationFilter> _filters;

        public BlockVaildationService(IEnumerable<IBlockValidationFilter> filters)
        {
            _filters = filters;
        }

        public async Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            int error = (int) ValidationError.Success; 
            foreach (var filter in _filters)
            {
                error = Math.Max((int)await filter.ValidateBlockAsync(block, context, keyPair), error);
                if (error == 3)
                    return ValidationError.InvalidBlock;
            }
            
            return (ValidationError) error;
        }
    }

    public interface IBlockValidationFilter
    {
        Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair);
    }
}