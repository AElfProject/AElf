using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.BlockValidationFilters;

namespace AElf.Kernel.Services
{
    public class BlockVaildationService: IBlockVaildationService
    {
        readonly IEnumerable<IBlockValidationFilter> _filters;

        public BlockVaildationService(IEnumerable<IBlockValidationFilter> filters)
        {
            _filters = filters;
        }

        public async Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context)
        {
            foreach (var filter in _filters)
            {
                var error = await filter.ValidateBlockAsync(block, context);
                if (error != ValidationError.Success)
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