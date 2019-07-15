using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.OS.BlockSync.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class InMemoryBlockDownloadJobStore : IBlockDownloadJobStore, ISingletonDependency
    {
        private readonly ConcurrentQueue<BlockDownloadJobInfo> _jobs = new ConcurrentQueue<BlockDownloadJobInfo>();

        public Task<bool> TryAddAsync(BlockDownloadJobInfo blockDownloadJobInfo)
        {
            if (_jobs.Count > 100)
                return Task.FromResult(false);

            _jobs.Enqueue(blockDownloadJobInfo);

            return Task.FromResult(true);
        }

        public Task<BlockDownloadJobInfo> GetFirstWaitingJobAsync()
        {
            _jobs.TryDequeue(out var blockDownloadJobInfo);

            return Task.FromResult(blockDownloadJobInfo);
        }
    }
}