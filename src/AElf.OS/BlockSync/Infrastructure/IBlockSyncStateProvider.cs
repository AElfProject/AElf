using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.BlockSync.Infrastructure;

public interface IBlockSyncStateProvider
{
    string LastRequestPeerPubkey { get; set; }
    Timestamp GetEnqueueTime(string queueName);

    void SetEnqueueTime(string queueName, Timestamp enqueueTime);

    bool TryUpdateDownloadJobTargetState(Hash targetHash, bool value);

    void SetDownloadJobTargetState(Hash targetHash, bool value);

    bool TryGetDownloadJobTargetState(Hash targetHash, out bool value);

    bool TryRemoveDownloadJobTargetState(Hash targetHash);
}