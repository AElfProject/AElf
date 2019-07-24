using System.Collections.Concurrent;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class BlockSyncStateProvider : IBlockSyncStateProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, Timestamp> _enqueueTimes;

        private readonly ConcurrentDictionary<Hash, bool> _downloadJobTargetState;

        public BlockSyncStateProvider()
        {
            _enqueueTimes = new ConcurrentDictionary<string, Timestamp>();
            _downloadJobTargetState = new ConcurrentDictionary<Hash, bool>();
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

        public bool TryUpdateDownloadJobTargetState(Hash targetHash, bool value)
        {
            return _downloadJobTargetState.TryUpdate(targetHash, value, !value);
        }

        public void SetDownloadJobTargetState(Hash targetHash, bool value)
        {
            _downloadJobTargetState[targetHash] = value;
        }

        public bool TryGetDownloadJobTargetState(Hash targetHash, out bool value)
        {
            return _downloadJobTargetState.TryGetValue(targetHash, out value);
        }

        public bool TryRemoveDownloadJobTargetState(Hash targetHash)
        {
            return _downloadJobTargetState.TryRemove(targetHash, out _);
        }
    }
}