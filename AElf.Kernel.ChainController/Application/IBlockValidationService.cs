using System.Threading.Tasks;

namespace AElf.Kernel.ChainController.Application
{
    public interface IBlockValidationService
    {
        Task<BlockValidationResult> ValidateBlockAsync(IBlock block);
    }
}