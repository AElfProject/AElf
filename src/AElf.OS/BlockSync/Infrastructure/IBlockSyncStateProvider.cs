using System.Collections.Concurrent;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IBlockSyncStateProvider
    {
        Timestamp GetEnqueueTime(string queueName);

        void SetEnqueueTime(string queueName, Timestamp enqueueTime);

        bool TryUpdateDownloadJobTargetState(Hash targetHash, bool value);

        void SetDownloadJobTargetState(Hash targetHash, bool value);

        bool TryGetDownloadJobTargetState(Hash targetHash, out bool value);

        bool TryRemoveDownloadJobTargetState(Hash targetHash);
    }
}