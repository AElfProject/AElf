using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;

namespace AElf.Services
{
    public interface IBlockVaildationService
    {
        Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair);
    }
}