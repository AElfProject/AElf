using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.BlockSync.Dto;
using AElf.OS.Network;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncService
    {
        Task SyncByAnnounceAsync(Chain chain, SyncAnnounceDto syncAnnounceDto);

        Task SyncByBlockAsync(BlockWithTransactions blockWithTransactions);
    }
}