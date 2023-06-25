using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Vote;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election;

public partial class ElectionContract
{
    public override Hash GetMinerElectionVotingItemId(Empty input)
    {
        return State.MinerElectionVotingItemId.Value;
    }

    public override PubkeyList GetCandidates(Empty input)
    {
        return State.Candidates.Value ?? new PubkeyList();
    }

    public override PubkeyList GetVotedCandidates(Empty input)
    {
        var votedCandidates = new PubkeyList();
        if (State.Candidates.Value == null) return votedCandidates;

        foreach (var pubkey in State.Candidates.Value.Value)
        {
            var candidateVotes = State.CandidateVotes[pubkey.ToHex()];
            if (candidateVotes != null && candidateVotes.ObtainedActiveVotedVotesAmount > 0)
                votedCandidates.Value.Add(pubkey);
        }

        return votedCandidates;
    }

    public override PubkeyList GetVictories(Empty input)
    {
        if (State.AEDPoSContract.Value == null)
            State.AEDPoSContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

        var currentMiners = State.AEDPoSContract.GetCurrentMinerList.Call(new Empty()).Pubkeys
            .Select(k => k.ToHex()).ToList();
        return new PubkeyList { Value = { GetVictories(currentMiners) } };
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
                new List<ByteString>(validCandidates.Select(ByteStringHelper.FromHexString));
            var backups = currentMiners.Where(k => !validCandidates.Contains(k)).ToList();
            if (State.InitialMiners.Value != null)
                backups.AddRange(
                    State.InitialMiners.Value.Value.Select(k => k.ToHex()).Where(k => !backups.Contains(k)));

            victories.AddRange(backups.OrderBy(p => p)
                .Take(Math.Min(diff, currentMiners.Count))
                .Select(ByteStringHelper.FromHexString));
            Context.LogDebug(() => string.Join("\n", victories.Select(v => v.ToHex().Substring(0, 10)).ToList()));
            return victories;
        }

        victories = validCandidates.Select(k => State.CandidateVotes[k])
            .OrderByDescending(v => v.ObtainedActiveVotedVotesAmount).Select(v => v.Pubkey)
            .Take(State.MinersCount.Value).ToList();
        Context.LogDebug(() => string.Join("\n", victories.Select(v => v.ToHex().Substring(0, 10)).ToList()));
        return victories;
    }

    private List<string> GetValidCandidates()
    {
        if (State.Candidates.Value == null) return new List<string>();

        return State.Candidates.Value.Value
            .Where(c => State.CandidateVotes[c.ToHex()] != null &&
                        State.CandidateVotes[c.ToHex()].ObtainedActiveVotedVotesAmount > 0)
            .Select(p => p.ToHex())
            .ToList();
    }

    public override Int32Value GetMinersCount(Empty input)
    {
        return new Int32Value { Value = State.MinersCount.Value };
    }

    public override ElectionResult GetElectionResult(GetElectionResultInput input)
    {
        var votingResult = State.VoteContract.GetVotingResult.Call(new GetVotingResultInput
        {
            VotingItemId = State.MinerElectionVotingItemId.Value,
            SnapshotNumber = input.TermNumber
        });

        var result = new ElectionResult
        {
            TermNumber = input.TermNumber,
            IsActive = input.TermNumber == State.CurrentTermNumber.Value,
            Results = { votingResult.Results }
        };

        return result;
    }

    public override CandidateInformation GetCandidateInformation(StringValue input)
    {
        return State.CandidateInformationMap[input.Value] ?? new CandidateInformation { Pubkey = input.Value };
    }

    public override TermSnapshot GetTermSnapshot(GetTermSnapshotInput input)
    {
        return State.Snapshots[input.TermNumber] ?? new TermSnapshot();
    }

    private TermSnapshot GetPreviousTermSnapshotWithNewestPubkey()
    {
        var termNumber = State.CurrentTermNumber.Value.Sub(1);
        var snapshot = State.Snapshots[termNumber];
        if (snapshot == null) return null;
        var invalidCandidates = snapshot.ElectionResult.Where(r => r.Value <= 0).Select(r => r.Key).ToList();
        Context.LogDebug(() => $"Invalid candidates count: {invalidCandidates.Count}");
        foreach (var invalidCandidate in invalidCandidates)
        {
            Context.LogDebug(() => $"Invalid candidate detected: {invalidCandidate}");
            if (snapshot.ElectionResult.ContainsKey(invalidCandidate)) snapshot.ElectionResult.Remove(invalidCandidate);
        }

        if (!snapshot.ElectionResult.Any()) return snapshot;

        var bannedCandidates = snapshot.ElectionResult.Keys.Where(IsPubkeyBanned).ToList();
        Context.LogDebug(() => $"Banned candidates count: {bannedCandidates.Count}");
        if (!bannedCandidates.Any()) return snapshot;
        Context.LogDebug(() => "Getting snapshot and there's miner replaced during current term.");
        foreach (var bannedCandidate in bannedCandidates)
        {
            var newestPubkey = GetNewestPubkey(bannedCandidate);
            // If newest pubkey not exists or same as old pubkey (which is banned), skip.
            if (newestPubkey == null || newestPubkey == bannedCandidate ||
                snapshot.ElectionResult.ContainsKey(newestPubkey)) continue;
            var electionResult = snapshot.ElectionResult[bannedCandidate];
            snapshot.ElectionResult.Add(newestPubkey, electionResult);
            if (snapshot.ElectionResult.ContainsKey(bannedCandidate)) snapshot.ElectionResult.Remove(bannedCandidate);
        }

        return snapshot;
    }

    public override ElectorVote GetElectorVote(StringValue input)
    {
        return GetElectorVote(input.Value);
    }

    public override ElectorVote GetElectorVoteWithRecords(StringValue input)
    {
        var votes = GetElectorVote(input.Value);
        
        if (votes.Address == null && votes.Pubkey == null)
            return votes;
        
        var votedRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
        {
            Ids = { votes.ActiveVotingRecordIds }
        }).Records;
        var index = 0;
        foreach (var record in votedRecords)
        {
            var voteId = votes.ActiveVotingRecordIds[index++];
            votes.ActiveVotingRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
        }

        return votes;
    }

    private ElectorVote GetElectorVote(string value)
    {
        Assert(value != null && value.Length > 1, "Invalid input.");
        
        var voterVotes = State.ElectorVotes[value];

        if (voterVotes == null && !AddressHelper.VerifyFormattedAddress(value))
        {
            voterVotes = State.ElectorVotes[
                Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(value)).ToBase58()];
        }

        return voterVotes ?? new ElectorVote();
    }

    public override ElectorVote GetElectorVoteWithAllRecords(StringValue input)
    {
        var votes = GetElectorVoteWithRecords(input);

        if (!votes.WithdrawnVotingRecordIds.Any()) return votes;

        var votedWithdrawnRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
        {
            Ids = { votes.WithdrawnVotingRecordIds }
        }).Records;
        var index = 0;
        foreach (var record in votedWithdrawnRecords)
        {
            var voteId = votes.WithdrawnVotingRecordIds[index++];
            votes.WithdrawnVotesRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
        }

        return votes;
    }

    public override Int64Value GetVotersCount(Empty input)
    {
        return new Int64Value
        {
            Value = State.VoteContract.GetLatestVotingResult.Call(State.MinerElectionVotingItemId.Value).VotersCount
        };
    }

    public override Int64Value GetVotesAmount(Empty input)
    {
        return new Int64Value
        {
            Value = State.VoteContract.GetLatestVotingResult.Call(State.MinerElectionVotingItemId.Value).VotesAmount
        };
    }

    public override GetPageableCandidateInformationOutput GetPageableCandidateInformation(PageInformation input)
    {
        var output = new GetPageableCandidateInformationOutput();
        var candidates = State.Candidates.Value;

        var count = candidates.Value.Count;
        if (count <= input.Start) return output;

        var length = Math.Min(Math.Min(input.Length, 20), candidates.Value.Count.Sub(input.Start));
        foreach (var candidate in candidates.Value.Skip(input.Start).Take(length))
            output.Value.Add(new CandidateDetail
            {
                CandidateInformation = State.CandidateInformationMap[candidate.ToHex()],
                ObtainedVotesAmount = GetCandidateVote(new StringValue { Value = candidate.ToHex() })
                    .ObtainedActiveVotedVotesAmount
            });

        return output;
    }

    public override CandidateVote GetCandidateVote(StringValue input)
    {
        return State.CandidateVotes[input.Value] ?? new CandidateVote
        {
            Pubkey = ByteStringHelper.FromHexString(input.Value)
        };
    }

    public override CandidateVote GetCandidateVoteWithRecords(StringValue input)
    {
        var votes = State.CandidateVotes[input.Value];
        if (votes == null)
            return new CandidateVote();

        var obtainedRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
        {
            Ids = { votes.ObtainedActiveVotingRecordIds }
        }).Records;
        var index = 0;
        foreach (var record in obtainedRecords)
        {
            var voteId = votes.ObtainedActiveVotingRecordIds[index++];
            votes.ObtainedActiveVotingRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
        }

        return votes;
    }

    public override CandidateVote GetCandidateVoteWithAllRecords(StringValue input)
    {
        var votes = GetCandidateVoteWithRecords(input);

        //get withdrawn records
        var obtainedWithdrawnRecords = State.VoteContract.GetVotingRecords.Call(new GetVotingRecordsInput
        {
            Ids = { votes.ObtainedWithdrawnVotingRecordIds }
        }).Records;
        var index = 0;
        foreach (var record in obtainedWithdrawnRecords)
        {
            var voteId = votes.ObtainedWithdrawnVotingRecordIds[index++];
            votes.ObtainedWithdrawnVotesRecords.Add(TransferVotingRecordToElectionVotingRecord(record, voteId));
        }

        return votes;
    }

    public override DataCenterRankingList GetDataCenterRankingList(Empty input)
    {
        return State.DataCentersRankingList.Value;
    }

    public override VoteWeightInterestList GetVoteWeightSetting(Empty input)
    {
        return State.VoteWeightInterestList.Value ?? GetDefaultVoteWeightInterest();
    }

    public override AuthorityInfo GetVoteWeightInterestController(Empty input)
    {
        if (State.VoteWeightInterestController.Value == null)
            return GetDefaultVoteWeightInterestController();
        return State.VoteWeightInterestController.Value;
    }

    public override VoteWeightProportion GetVoteWeightProportion(Empty input)
    {
        return State.VoteWeightProportion.Value ?? GetDefaultVoteWeightProportion();
    }

    public override Int64Value GetCalculateVoteWeight(VoteInformation input)
    {
        return new Int64Value
        {
            Value = GetVotesWeight(input.Amount, input.LockTime)
        };
    }

    private ElectionVotingRecord TransferVotingRecordToElectionVotingRecord(VotingRecord votingRecord, Hash voteId)
    {
        var lockSeconds = State.LockTimeMap[voteId];
        return new ElectionVotingRecord
        {
            Voter = votingRecord.Voter,
            Candidate = GetNewestPubkey(votingRecord.Option),
            Amount = votingRecord.Amount,
            TermNumber = votingRecord.SnapshotNumber,
            VoteId = voteId,
            LockTime = lockSeconds,
            VoteTimestamp = votingRecord.VoteTimestamp,
            WithdrawTimestamp = votingRecord.WithdrawTimestamp,
            UnlockTimestamp = votingRecord.VoteTimestamp.AddSeconds(lockSeconds),
            IsWithdrawn = votingRecord.IsWithdrawn,
            Weight = GetVotesWeight(votingRecord.Amount, lockSeconds),
            IsChangeTarget = votingRecord.IsChangeTarget
        };
    }

    public override MinerReplacementInformation GetMinerReplacementInformation(
        GetMinerReplacementInformationInput input)
    {
        var evilMinersPubKeys = GetEvilMinersPubkeys(input.CurrentMinerList);
        Context.LogDebug(() => $"Got {evilMinersPubKeys.Count} evil miners pubkeys from {input.CurrentMinerList}");
        var alternativeCandidates = new List<string>();
        var latestSnapshot = GetPreviousTermSnapshotWithNewestPubkey();
        // Check out election snapshot.
        if (latestSnapshot != null && latestSnapshot.ElectionResult.Any())
        {
            Context.LogDebug(() => $"Previous term snapshot:\n{latestSnapshot}");
            var maybeNextCandidates = latestSnapshot.ElectionResult
                // Except initial miners.
                .Where(cs =>
                    !State.InitialMiners.Value.Value.Contains(
                        ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(cs.Key))))
                // Except current miners.
                .Where(cs => !input.CurrentMinerList.Contains(cs.Key))
                .OrderByDescending(s => s.Value).ToList();
            var take = Math.Min(evilMinersPubKeys.Count, maybeNextCandidates.Count);
            alternativeCandidates.AddRange(maybeNextCandidates.Select(c => c.Key).Take(take));
            Context.LogDebug(() =>
                $"Found alternative miner from candidate list: {alternativeCandidates.Aggregate("\n", (key1, key2) => key1 + "\n" + key2)}");
        }

        // If the count of evil miners is greater than alternative candidates, add some initial miners to alternative candidates.
        var diff = evilMinersPubKeys.Count - alternativeCandidates.Count;
        if (diff > 0)
        {
            var takeAmount = Math.Min(diff, State.InitialMiners.Value.Value.Count);
            var selectedInitialMiners = State.InitialMiners.Value.Value
                .Select(k => k.ToHex())
                .Where(k => !State.BannedPubkeyMap[k])
                .Where(k => !input.CurrentMinerList.Contains(k)).Take(takeAmount);
            alternativeCandidates.AddRange(selectedInitialMiners);
        }

        return new MinerReplacementInformation
        {
            EvilMinerPubkeys = { evilMinersPubKeys },
            AlternativeCandidatePubkeys = { alternativeCandidates }
        };
    }

    private List<string> GetEvilMinersPubkeys(IEnumerable<string> currentMinerList)
    {
        return currentMinerList.Where(p => State.BannedPubkeyMap[p]).ToList();
    }

    private int GetValidationDataCenterCount()
    {
        return GetMinersCount(new Empty()).Value.Mul(5);
    }

    public override Address GetCandidateAdmin(StringValue input)
    {
        return State.CandidateAdmins[State.InitialPubkeyMap[input.Value] ?? input.Value];
    }

    public override StringValue GetReplacedPubkey(StringValue input)
    {
        return new StringValue { Value = State.CandidateReplacementMap[input.Value] };
    }

    public override Address GetSponsor(StringValue input)
    {
        return State.CandidateSponsorMap[input.Value] ??
               Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(input.Value));
    }

    public override PubkeyList GetManagedPubkeys(Address input)
    {
        return State.ManagedCandidatePubkeysMap[input];
    }
}