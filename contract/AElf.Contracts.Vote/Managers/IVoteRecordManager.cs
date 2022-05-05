using AElf.Types;
using Google.Protobuf.Collections;

namespace AElf.Contracts.Vote.Managers
{
    public interface IVoteRecordManager
    {
        VotingRecord AddVoteRecord(Hash voteId, Hash votingItemId, long amount, long snapshotNumber,string option, Address voter, bool isChangerTarget);

        void RemoveVoteRecord();

        void UpdateVoteRecord(Hash voteId, VotingRecord votingRecord);

        VotingRecord GetVoteRecord(Hash voteId);
    }
}