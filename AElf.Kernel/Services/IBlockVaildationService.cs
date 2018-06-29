using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.BlockValidationFilters;
using AElf.Kernel.Types;

namespace AElf.Kernel.Services
{
    public interface IBlockVaildationService
    {
        Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair);
    }
}