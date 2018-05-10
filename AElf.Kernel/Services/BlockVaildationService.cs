using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Services
{
    public class BlockVaildationService: IBlockVaildationService
    {
        readonly IEnumerable<IBlockValidationFilter> _filters;

        public BlockVaildationService(IEnumerable<IBlockValidationFilter> filters)
        {
            _filters = filters;
        }

        public async Task<bool> ValidateBlockAsync(Block block, IChainContext context)
        {
            foreach (var filter in _filters)
            {
                if (!await filter.ValidateBlockAsync(block, context))
                    return false;
            }

            return true;
        }

    }

    public interface IBlockValidationFilter
    {
        Task<bool> ValidateBlockAsync(Block block, IChainContext context);
    }
}