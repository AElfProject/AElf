using System.Threading.Tasks;

namespace AElf.Kernel.BlockValidationFilters
{
    public class ConsensusBlockValidationFilter: IBlockValidationFilter
    {
        public Task<bool> ValidateBlockAsync(Block block, IChainContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}