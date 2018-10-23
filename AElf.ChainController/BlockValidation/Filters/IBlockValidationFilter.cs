using System.Threading.Tasks;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public interface IBlockValidationFilter
    {
        Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context);
    }
}