using System.Collections.Concurrent;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class BlockSyncStateProvider : IBlockSyncStateProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, Timestamp> _enqueueTimes;

        public BlockSyncStateProvider()
        {
            _enqueueTimes = new ConcurrentDictionary<string, Timestamp>();
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