using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncService
    {
        Task SyncBlockAsync(Chain chain, SyncBlockDto syncBlockDto);

        Task SyncByBlockAsync(BlockWithTransactions blockWithTransactions);
    }
}