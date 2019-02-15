using System.Threading.Tasks;

namespace AElf.Kernel.ChainController.Application.Filters
{
    public interface IBlockValidationFilter
    {
        Task<BlockValidationResult> ValidateBlockAsync(IBlock block);
    }
}