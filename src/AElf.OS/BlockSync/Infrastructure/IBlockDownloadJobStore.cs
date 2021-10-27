using System.Threading.Tasks;
using AElf.OS.BlockSync.Types;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IBlockDownloadJobStore
    {
        Task<bool> AddAsync(BlockDownloadJobInfo blockDownloadJobInfo);

        Task<BlockDownloadJobInfo> GetFirstWaitingJobAsync();

        Task UpdateAsync(BlockDownloadJobInfo blockDownloadJobInfo);

        Task RemoveAsync(string jobId);
    }
}