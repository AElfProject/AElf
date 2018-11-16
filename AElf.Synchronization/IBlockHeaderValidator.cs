using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Synchronization
{
    public interface IBlockHeaderValidator
    {
        Task<bool> CheckLinkabilityAsync(BlockHeader blockHeader);
        Task<BlockHeaderValidationResult> ValidateBlockHeaderAsync(BlockHeader blockHeader);
    }
}