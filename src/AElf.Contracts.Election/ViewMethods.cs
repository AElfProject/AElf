using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Contracts.Vote;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract
    {
        public override Hash GetMinerElectionVotingItemId(Empty input)
        {
            return State.MinerElectionVotingItemId.Value;
        }

        public override PublicKeysList GetCandidates(Empty input)
        {
            return State.Candidates.Value;
        }

        public override PublicKeysList GetVictories(Empty input)
        {
            var currentMiners = State.AEDPoSContract.GetPreviousRoundInformation.Call(new Empty())
                .RealTimeMinersInformation.Keys.ToList();
            return new PublicKeysList {Value = {GetVictories(currentMiners)}};
        }

        private List<ByteString> GetVictories(List<string> currentMiners)
        {
            var validCandidates = GetValidCandidates();

            List<ByteString> victories;

            Context.LogDebug(() => $"Valid candidates: {validCandidates.Count} / {State.MinersCount.Value}");

            var diff = State.MinersCount.Value - validCandidates.Count;
            // Valid candidates not enough.
            if (diff > 0)
            {
                victories =
                    new List<ByteString>(validCandidates.Select(vc =>
                        ByteString.CopyFrom(ByteArrayHelpers.FromHexString(vc))));
                victories.AddRange(currentMiners.Where(k => !validCandidates.Contains(k)).OrderBy(p => p)
                    .Take(Math.Min(diff, currentMiners.Count))
                    .Select(p => ByteString.CopyFrom(ByteArrayHelpers.FromHexString(p))));
                Context.LogDebug(() => string.Join("\n", victories.Select(v => v.ToHex().Substring(0, 10)).ToList()));
                return victories;
            }

            victories = validCandidates.Select(k => State.CandidateVotes[k])
                .OrderByDescending(v => v.ObtainedActiveVotedVotesAmount).Select(v => v.PublicKey)
                .Take(State.MinersCount.Value).ToList();
            Context.LogDebug(() => string.Join("\n", victories.Select(v => v.ToHex().Substring(0, 10)).ToList()));
            return victories;
        }

        private List<string> GetValidCandidates()
        {
            return State.Candidates.Value.Value
                .Where(c => State.CandidateVotes[c.ToHex()] != null &&
                            State.CandidateVotes[c.ToHex()].ObtainedActiveVotedVotesAmount > 0)
                .Select(p => p.ToHex())
                .ToList();
        }

        public override SInt32Value GetMinersCount(Empty input)
        {
            return new SInt32Value {Value = State.MinersCount.Value};
        }

        public override ElectionResult GetElectionResult(GetElectionResultInput input)
        {
            var votingResult = State.VoteContract.GetVotingResult.Call(new GetVotingResultInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value,
                SnapshotNumber = input.TermNumber,
            });

            var result = new ElectionResult
            {
                TermNumber = input.TermNumber,
                IsActive = input.TermNumber == State.CurrentTermNumber.Value,
                Results = {votingResult.Results}
            };

            return result;
        }

        public override CandidateInformation GetCandidateInformation(StringInput input)
        {
            return State.CandidateInformationMap[input.Value] ?? new CandidateInformation {PublicKey = input.Value};
        }

        public override TermSnapshot GetTermSnapshot(GetTermSnapshotInput input)
        {
            return State.Snapshots[input.TermNumber] ?? new TermSnapshot();
        }

        public override ElectorVote GetElectorVote(StringInput input)
        {
            return State.ElectorVotes[input.Value] ?? new ElectorVote
            {
                PublicKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Value))
            };
        }

        public override ElectorVote GetElectorVoteWithRecords(StringInput input)
        {
            var votes = State.ElectorVotes[input.Value];
            if (votes == null) return new ElectorVote
            {
                PublicKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Value))
            };

            var votedRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
            {
                Ids = {votes.ActiveVotingRecordIds}
            }).Records;
            var index = 0;
            foreach (var record in votedRecords)
            {
                var voteId = votes.ActiveVotingRecordIds[index++];
                votes.ActiveVotingRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
            }

            return votes;
        }

        public override ElectorVote GetElectorVoteWithAllRecords(StringInput input)
        {
            var votes = GetElectorVoteWithRecords(input);

            if (!votes.WithdrawnVotesRecords.Any())
            {
                return votes;
            }

            var votedWithdrawnRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
            {
                Ids = {votes.WithdrawnVotingRecordIds}
            }).Records;
            var index = 0;
            foreach (var record in votedWithdrawnRecords)
            {
                var voteId = votes.WithdrawnVotingRecordIds[index++];
                votes.WithdrawnVotesRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
            }

            return votes;
        }
        
        public override CandidateVote GetCandidateVote(StringInput input)
        {
            return State.CandidateVotes[input.Value] ?? new CandidateVote
            {
                PublicKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Value))
            };
        }

        public override CandidateVote GetCandidateVoteWithRecords(StringInput input)
        {
            var votes = State.CandidateVotes[input.Value];
            if (votes == null)
                return new CandidateVote
                {
                    PublicKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Value))
                };

            var obtainedRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
            {
                Ids = {votes.ObtainedActiveVotingRecordIds}
            }).Records;
            var index = 0;
            foreach (var record in obtainedRecords)
            {
                var voteId = votes.ObtainedActiveVotingRecordIds[index++];
                votes.ObtainedActiveVotingRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
            }

            return votes;
        }

        public override CandidateVote GetCandidateVoteWithAllRecords(StringInput input)
        {
            var votes = GetCandidateVoteWithRecords(input);

            if (!votes.ObtainedWithdrawnVotesRecords.Any())
            {
                return votes;
            }

            var obtainedWithdrawnRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
            {
                Ids = {votes.ObtainedWithdrawnVotingRecordIds}
            }).Records;
            var index = 0;
            foreach (var record in obtainedWithdrawnRecords)
            {
                var voteId = votes.ObtainedWithdrawnVotingRecordIds[index++];
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
                TermNumber = votingRecord.SnapshotNumber,
                VoteId = voteId,
                LockTime = (int) lockDays,
                VoteTimestamp = votingRecord.VoteTimestamp,
                WithdrawTimestamp = votingRecord.WithdrawTimestamp,
                UnlockTimestamp = votingRecord.VoteTimestamp.AddDays(lockDays),
                IsWithdrawn = votingRecord.IsWithdrawn
            };
        }
    }
}