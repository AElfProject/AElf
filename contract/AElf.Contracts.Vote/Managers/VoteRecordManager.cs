using AElf.Contracts.Vote.Managers;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.Collections;

namespace AElf.Contracts.Vote
{
    public class VoteRecordManager:IVoteRecordManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly MappedState<Hash, VotingRecord> _votingRecordMap;

        public VoteRecordManager(CSharpSmartContractContext context, MappedState<Hash, VotingRecord> votingRecordMap)
        {
            _context = context;
            _votingRecordMap = votingRecordMap;
        }

        public VotingRecord AddVoteRecord(Hash voteId, Hash votingItemId, long amount,long snapshotNumber, string option, Address voter,
            bool isChangerTarget)
        {
            var votingRecord = new VotingRecord
            {
                VotingItemId = votingItemId,
                Amount = amount,
                SnapshotNumber = snapshotNumber,
                Option = option,
                IsWithdrawn = false,
                VoteTimestamp = _context.CurrentBlockTime,
                Voter = voter,
                IsChangeTarget = isChangerTarget
            };
            _votingRecordMap[voteId] = votingRecord;
            return votingRecord;
        }

        public void RemoveVoteRecord()
        {
            throw new System.NotImplementedException();
        }

        public void UpdateVoteRecord(Hash voteId, VotingRecord votingRecord)
        {
            _votingRecordMap[voteId] = votingRecord;
        }


        public VotingRecord GetVoteRecord(Hash voteId)
        {
            var votingRecord = _votingRecordMap[voteId];
            if (votingRecord == null)
            {
                throw new AssertionException("Voting record not found.");
            }

            return votingRecord;
        }
    }
}