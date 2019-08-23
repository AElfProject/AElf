using System.Threading.Tasks;
using AElf.OS.BlockSync.Dto;
using AElf.OS.BlockSync.Types;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockDownloadService
    {
        Task<DownloadBlocksResult> DownloadBlocksAsync(DownloadBlockDto downloadBlockDto);

        bool ValidateQueueAvailabilityBeforeDownload();

        void RemoveDownloadJobTargetState(Hash targetBlockHash);

        bool IsNotReachedDownloadTarget(Hash targetBlockHash);
    }
}