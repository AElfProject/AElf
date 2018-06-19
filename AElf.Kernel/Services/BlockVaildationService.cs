using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.BlockValidationFilters;
using AElf.Kernel.Managers;

namespace AElf.Kernel.Services
{
    public class BlockVaildationService: IBlockVaildationService
    {
        private readonly IEnumerable<IBlockValidationFilter> _filters;
        private readonly IBlockManager _blockManager;

        public BlockVaildationService(IEnumerable<IBlockValidationFilter> filters, IBlockManager blockManager)
        {
            _filters = filters;
            _blockManager = blockManager;
        }

        public async Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context)
        {
            foreach (var filter in _filters)
            {
                var error = await filter.ValidateBlockAsync(block, context);
                if (error == ValidationError.Success) continue;
                if (error == ValidationError.OrphanBlock)
                {
                    await _blockManager.AddBlockAsync((Block)block);
                }
                return error;

            }
            
            return ValidationError.Success;
        }
    }

    public interface IBlockValidationFilter
    {
        Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context);
    }
}