using AElf.Contracts.Vote;
using AElf.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Profit;
using AElf.Sdk.CSharp;
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
                    new List<ByteString>(validCandidates.Select(vc => vc.ToByteString()));
                victories.AddRange(currentMiners.Where(k => !validCandidates.Contains(k)).OrderBy(p => p)
                    .Take(Math.Min(diff, currentMiners.Count))
                    .Select(p => p.ToByteString()));
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
                PublicKey = input.Value.ToByteString()
            };
        }

        public override ElectorVote GetElectorVoteWithRecords(StringInput input)
        {
            var votes = State.ElectorVotes[input.Value];
            if (votes == null) return new ElectorVote
            {
                PublicKey = input.Value.ToByteString()
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

        public override SInt64Value GetVotersCount(Empty input)
        {
            return new SInt64Value
            {
                Value = State.VoteContract.GetLatestVotingResult.Call(State.MinerElectionVotingItemId.Value).VotersCount
            };
        }

        public override SInt64Value GetVotesAmount(Empty input)
        {
            return new SInt64Value
            {
                Value = State.VoteContract.GetLatestVotingResult.Call(State.MinerElectionVotingItemId.Value).VotesAmount
            };
        }

        public override SInt64Value GetCurrentMiningReward(Empty input)
        {
            return new SInt64Value
            {
                Value = State.AEDPoSContract.GetCurrentRoundInformation.Call(new Empty()).RealTimeMinersInformation
                    .Values.Sum(minerInRound => minerInRound.ProducedBlocks).Mul(ElectionContractConstants.ElfTokenPerBlock)
            };
        }

        public override GetPageableCandidateInformationOutput GetPageableCandidateInformation(PageInformation input)
        {
            var output = new GetPageableCandidateInformationOutput();
            var candidates = State.Candidates.Value;
            var length = Math.Min(Math.Min(input.Length, 20), candidates.Value.Count.Sub(input.Start));
            foreach (var candidate in candidates.Value.Skip(input.Start).Take(length))
            {
                output.Value.Add(new CandidateDetail
                {
                    CandidateInformation = State.CandidateInformationMap[candidate.ToHex()],
                    ObtainedVotesAmount = State.CandidateVotes[candidate.ToHex()].ObtainedActiveVotedVotesAmount
                });
            }

            return output;
        }

        public override CandidateVote GetCandidateVote(StringInput input)
        {
            return State.CandidateVotes[input.Value] ?? new CandidateVote
            {
                PublicKey = input.Value.ToByteString()
            };
        }

        public override CandidateVote GetCandidateVoteWithRecords(StringInput input)
        {
            var votes = State.CandidateVotes[input.Value];
            if (votes == null)
                return new CandidateVote
                {
                    PublicKey = input.Value.ToByteString()
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
            var lockSeconds = State.LockTimeMap[voteId];
            return new ElectionVotingRecord
            {
                Voter = votingRecord.Voter,
                Candidate = votingRecord.Option,
                Amount = votingRecord.Amount,
                TermNumber = votingRecord.SnapshotNumber,
                VoteId = voteId,
                LockTime = lockSeconds,
                VoteTimestamp = votingRecord.VoteTimestamp,
                WithdrawTimestamp = votingRecord.WithdrawTimestamp,
                UnlockTimestamp = votingRecord.VoteTimestamp + new Duration{Seconds = lockSeconds},
                IsWithdrawn = votingRecord.IsWithdrawn
            };
        }
    }
}