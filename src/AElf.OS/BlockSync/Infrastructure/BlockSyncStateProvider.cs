using System.Collections.Concurrent;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class BlockSyncStateProvider : IBlockSyncStateProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, Timestamp> _enqueueTimes;

        public ConcurrentDictionary<Hash, bool> DownloadJobTargetState { get; set; }

        public BlockSyncStateProvider()
        {
            _enqueueTimes = new ConcurrentDictionary<string, Timestamp>();
            DownloadJobTargetState = new ConcurrentDictionary<Hash, bool>();
        }

        public Timestamp GetEnqueueTime(string queueName)
        {
            if (_enqueueTimes.TryGetValue(queueName, out var enqueueTime))
            {
                return enqueueTime;
            }

            return null;
        }

        public void SetEnqueueTime(string queueName, Timestamp enqueueTime)
        {
            _enqueueTimes[queueName] = enqueueTime;
        }
    }
}