using System.Threading.Tasks;
using AElf.Kernel.Services;

namespace AElf.Kernel.BlockValidationFilters
{
    public class ConsensusBlockValidationFilter: IBlockValidationFilter
    {
        public Task<bool> ValidateBlockAsync(Block block, IChainContext context)
        {
            return Task.FromResult(true);
        }
    }
}