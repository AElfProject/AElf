using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public interface IBlockValidationService
    {
        Task<BlockValidationResult> ValidateBlockAsync(IBlock block, IChainContext context);
    }
}