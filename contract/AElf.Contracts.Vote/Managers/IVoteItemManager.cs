using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote.Managers
{
    public interface IVoteItemManager
    {
        void AddVoteItem(Hash votingItemId, string acceptedCurrency, bool isLockToken,
            long totalSnapshotNumber, Timestamp startTimestamp, Timestamp endTimestamp, RepeatedField<string> options);

        void RemoveVoteItem(Hash votingItemId);

        long UpdateSnapshotNumber(Hash votingItemId);

        void AddOptions(Hash votingItemId,RepeatedField<string> options);

        void RemoveOptions(Hash votingItemId, RepeatedField<string> options);

        VotingItem GetVotingItem(Hash votingItemId);

    }
}