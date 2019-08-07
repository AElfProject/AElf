using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncTransactionValidationProvider
    {
        Task<bool> ValidateTransactionAsync(Transaction transaction);
    }
}