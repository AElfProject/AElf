using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncValidationService
    {
        Task<bool> ValidateBeforeSync(Chain chain, Hash syncBlockHash, long syncBlockHeight);
        
        Task<bool> ValidateBeforeHandleBlockAsync(Chain chain, BlockWithTransactions blockWithTransactions);
    }
}