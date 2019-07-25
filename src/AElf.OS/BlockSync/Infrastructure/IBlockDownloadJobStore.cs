using System.Threading.Tasks;
using AElf.OS.BlockSync.Types;
using AElf.Types;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IBlockDownloadJobStore
    {
        Task<bool> AddAsync(BlockDownloadJobInfo blockDownloadJobInfo);

        Task<BlockDownloadJobInfo> GetFirstWaitingJobAsync();

        Task UpdateAsync(BlockDownloadJobInfo blockDownloadJobInfo);

        Task RemoveAsync(Hash jobId);
    }
}