using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncValidationService
    {
        Task<bool> ValidateBeforeEnqueue(Hash syncBlockHash, long syncBlockHeight);
    }
}