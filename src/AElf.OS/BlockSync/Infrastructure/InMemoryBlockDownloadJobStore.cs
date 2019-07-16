using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.OS.BlockSync.Types;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class InMemoryBlockDownloadJobStore : IBlockDownloadJobStore, ISingletonDependency
    {
        private readonly ConcurrentQueue<Hash> _jobIds = new ConcurrentQueue<Hash>();
        
        private readonly ConcurrentDictionary<Hash,BlockDownloadJobInfo> _jobs = new ConcurrentDictionary<Hash, BlockDownloadJobInfo>();

        public Task<bool> AddAsync(BlockDownloadJobInfo blockDownloadJobInfo)
        {
            if (_jobs.Count > 100)
                return Task.FromResult(false);

            _jobIds.Enqueue(blockDownloadJobInfo.JobId);
            _jobs[blockDownloadJobInfo.JobId] = blockDownloadJobInfo;
            
            return Task.FromResult(true);
        }

        public Task<BlockDownloadJobInfo> GetFirstWaitingJobAsync()
        {
            while (true)
            {
                if (!_jobIds.TryPeek(out var jobId))
                {
                    return Task.FromResult<BlockDownloadJobInfo>(null);
                }
                
                if(_jobs.TryGetValue(jobId, out var blockDownloadJobInfo))
                {
                    return Task.FromResult(blockDownloadJobInfo);
                }

                _jobIds.TryDequeue(out _);
            }
        }

        public Task UpdateAsync(BlockDownloadJobInfo blockDownloadJobInfo)
        {
            _jobs[blockDownloadJobInfo.JobId] = blockDownloadJobInfo;
            return Task.FromResult(0);
        }

        public Task RemoveAsync(Hash jobId)
        {
            _jobs.TryRemove(jobId, out _);
            return Task.FromResult(0);
        }
    }
}