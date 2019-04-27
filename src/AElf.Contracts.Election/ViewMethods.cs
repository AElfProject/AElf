using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract
    {
        public override PublicKeysList GetVictories(Empty input)
        {
            var diff = State.MinersCount.Value - State.Candidates.Value.Value.Count;
            if (diff > 0)
            {
                var currentMiners = State.AElfConsensusContract.GetPreviousRoundInformation.Call(new Empty())
                    .RealTimeMinersInformation.Keys.ToList();
                var victories = new PublicKeysList {Value = {State.Candidates.Value.Value}};
                victories.Value.AddRange(currentMiners.Where(k => !currentMiners.Contains(k)).OrderBy(k => k).Take(diff)
                    .Select(k => ByteString.CopyFrom(ByteArrayHelpers.FromHexString(k))));
                return victories;
            }

            return new PublicKeysList
            {
                Value =
                {
                    State.Candidates.Value.Value.Select(p => p.ToHex()).Select(k => State.Votes[k])
                        .Where(v => v != null)
                        .OrderByDescending(v => v.ValidObtainedVotesAmount).Select(v => v.PublicKey)
                        .Take(State.MinersCount.Value)
                }
            };
        }

        private List<string> GetVictories(List<string> currentMiners)
        {
            var diff = State.MinersCount.Value - State.Candidates.Value.Value.Count;
            if (diff > 0)
            {
                var victories = new List<string>();
                victories.AddRange(currentMiners.Where(k => !currentMiners.Contains(k)).OrderBy(k => k).Take(diff));
                return victories;
            }

            return State.Candidates.Value.Value.Select(p => p.ToHex()).Select(k => State.Votes[k])
                .Where(v => v != null)
                .OrderByDescending(v => v.ValidObtainedVotesAmount).Select(v => v.PublicKey.ToHex())
                .Take(State.MinersCount.Value).ToList();
        }

        public override CandidateHistory GetCandidateHistory(StringInput input)
        {
            return State.Histories[input.Value] ?? new CandidateHistory();
        }

        public override TermSnapshot GetTermSnapshot(GetTermSnapshotInput input)
        {
            return State.Snapshots[input.TermNumber] ?? new TermSnapshot();
        }

        public override Votes GetVotesInformation(StringInput input)
        {
            return State.Votes[input.Value] ?? new Votes();
        }

        public override Votes GetVotesInformationWithRecords(StringInput input)
        {
            var votes = State.Votes[input.Value];
            if (votes == null) return new Votes();

            var votedRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
            {
                Ids = {votes.ActiveVotesIds}
            }).Records;
            var index = 0;
            foreach (var record in votedRecords)
            {
                var voteId = votes.ActiveVotesIds[index++];
                votes.ActiveVotesRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
            }

            var obtainedRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
            {
                Ids = {votes.ObtainedActiveVotesIds}
            }).Records;
            index = 0;
            foreach (var record in obtainedRecords)
            {
                var voteId = votes.ActiveVotesIds[index++];
                votes.ObtainedActiveVotesRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
            }

            return votes;
        }

        public override Votes GetVotesInformationWithAllRecords(StringInput input)
        {
            var votes = GetVotesInformationWithRecords(input);

            var votedWithdrawnRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
            {
                Ids = {votes.WithdrawnVotesIds}
            }).Records;
            var index = 0;
            foreach (var record in votedWithdrawnRecords)
            {
                var voteId = votes.ActiveVotesIds[index++];
                votes.WithdrawnVotesRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
            }

            var obtainedWithdrawnRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
            {
                Ids = {votes.ObtainedWithdrawnVotesIds}
            }).Records;
            index = 0;
            foreach (var record in obtainedWithdrawnRecords)
            {
                var voteId = votes.ActiveVotesIds[index++];
                votes.ObtainedWithdrawnVotesRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
            }

            return votes;
        }

        private ElectionVotingRecord TransferVotingRecordToElectionVotingRecord(VotingRecord votingRecord, Hash voteId)
        {
            var lockDays = State.LockTimeMap[voteId];
            return new ElectionVotingRecord
            {
                Voter = votingRecord.Voter,
                Candidate = votingRecord.Option,
                Amount = votingRecord.Amount,
                TermNumber = votingRecord.EpochNumber,
                VoteId = voteId,
                LockTime = lockDays,
                VoteTimestamp = votingRecord.VoteTimestamp,
                WithdrawTimestamp = votingRecord.WithdrawTimestamp,
                UnlockTimestamp = votingRecord.VoteTimestamp.ToDateTime().AddDays(lockDays).ToTimestamp(),
                IsWithdrawn = votingRecord.IsWithdrawn
            };
        }
    }
}