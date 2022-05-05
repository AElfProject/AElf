using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote.Services
{
    public interface IVoteService
    {
        void RegisterVote(Hash votingItemId, string acceptedCurrency, bool isLockToken,
            long totalSnapshotNumber, Timestamp startTimestamp, Timestamp endTimestamp, RepeatedField<string> options);

        void Vote(Hash voteId, Hash votingItemId, long amount, string option, Address voter,
            bool isChangerTarget);

         void Withdraw(Hash voteId);

         void TakeSnapshot(Hash votingItemId, long snapshotNumber);
        void Claim();
    }
}