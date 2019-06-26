using System.Threading.Tasks;
using AElf.OS.BlockSync.Dto;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncService
    {
        Task SyncBlockAsync(SyncBlockDto syncBlockDto);

        void EnqueueSyncBlockJob(SyncBlockDto syncBlockDto);
    }
}