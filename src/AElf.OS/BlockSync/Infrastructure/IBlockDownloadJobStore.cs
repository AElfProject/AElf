using System.Threading.Tasks;
using AElf.OS.BlockSync.Types;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IBlockDownloadJobStore
    {
        Task<bool> TryAddAsync(BlockDownloadJobInfo blockDownloadJobInfo);

        Task<BlockDownloadJobInfo> GetFirstWaitingJobAsync();
    }
}