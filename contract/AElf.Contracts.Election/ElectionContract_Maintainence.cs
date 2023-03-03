using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election;

public partial class ElectionContract : ElectionContractImplContainer.ElectionContractImplBase
{
    /// <summary>
    ///     Initialize the ElectionContract and corresponding contract states.
    /// </summary>
    /// <param name="input">InitialElectionContractInput</param>
    /// <returns></returns>
    public override Empty InitialElectionContract(InitialElectionContractInput input)
    {
        Assert(!State.Initialized.Value, "Already initialized.");

        State.Candidates.Value = new PubkeyList();

        State.MinimumLockTime.Value = input.MinimumLockTime;
        State.MaximumLockTime.Value = input.MaximumLockTime;

        State.TimeEachTerm.Value = input.TimeEachTerm;

        State.MinersCount.Value = input.MinerList.Count;
        State.InitialMiners.Value = new PubkeyList
        {
            Value = { input.MinerList.Select(ByteStringHelper.FromHexString) }
        };
        foreach (var pubkey in input.MinerList)
            State.CandidateInformationMap[pubkey] = new CandidateInformation
            {
                Pubkey = pubkey
            };

        State.CurrentTermNumber.Value = 1;

        State.DataCentersRankingList.Value = new DataCenterRankingList();

        State.Initialized.Value = true;

        return new Empty();
    }

    public override Empty RegisterElectionVotingEvent(Empty input)
    {
        Assert(!State.VotingEventRegistered.Value, "Already registered.");

        State.VoteContract.Value = Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName);

        var votingRegisterInput = new VotingRegisterInput
        {
            IsLockToken = false,
            AcceptedCurrency = Context.Variables.NativeSymbol,
            TotalSnapshotNumber = long.MaxValue,
            StartTimestamp = TimestampHelper.MinValue,
            EndTimestamp = TimestampHelper.MaxValue
        };
        State.VoteContract.Register.Send(votingRegisterInput);

        State.MinerElectionVotingItemId.Value = HashHelper.ConcatAndCompute(
            HashHelper.ComputeFrom(votingRegisterInput),
            HashHelper.ComputeFrom(Context.Self));

        State.VotingEventRegistered.Value = true;
        return new Empty();
    }

    /// <summary>
    ///     Update the candidate information,if it's not evil node.
    /// </summary>
    /// <param name="input">UpdateCandidateInformationInput</param>
    /// <returns></returns>
    public override Empty UpdateCandidateInformation(UpdateCandidateInformationInput input)
    {
        Assert(
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName) ==
            Context.Sender || Context.Sender == GetEmergencyResponseOrganizationAddress(),
            "Only consensus contract can update candidate information.");

        var candidateInformation = State.CandidateInformationMap[input.Pubkey];
        if (candidateInformation == null) return new Empty();

        if (input.IsEvilNode)
        {
            var publicKeyByte = ByteArrayHelper.HexStringToByteArray(input.Pubkey);
            State.BannedPubkeyMap[input.Pubkey] = true;
            var rankingList = State.DataCentersRankingList.Value;
            if (rankingList.DataCenters.ContainsKey(input.Pubkey))
            {
                rankingList.DataCenters[input.Pubkey] = 0;
                UpdateDataCenterAfterMemberVoteAmountChanged(rankingList, input.Pubkey, true);
                State.DataCentersRankingList.Value = rankingList;
            }

            Context.LogDebug(() => $"Marked {input.Pubkey.Substring(0, 10)} as an evil node.");
            Context.Fire(new EvilMinerDetected { Pubkey = input.Pubkey });
            State.CandidateInformationMap.Remove(input.Pubkey);
            var candidates = State.Candidates.Value;
            candidates.Value.Remove(ByteString.CopyFrom(publicKeyByte));
            State.Candidates.Value = candidates;
            RemoveBeneficiary(input.Pubkey);
            return new Empty();
        }

        candidateInformation.ProducedBlocks = candidateInformation.ProducedBlocks.Add(input.RecentlyProducedBlocks);
        candidateInformation.MissedTimeSlots =
            candidateInformation.MissedTimeSlots.Add(input.RecentlyMissedTimeSlots);
        State.CandidateInformationMap[input.Pubkey] = candidateInformation;
        return new Empty();
    }

    private Address GetEmergencyResponseOrganizationAddress()
    {
        if (State.EmergencyResponseOrganizationAddress.Value != null)
            return State.EmergencyResponseOrganizationAddress.Value;

        if (State.ParliamentContract.Value == null)
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);

        State.EmergencyResponseOrganizationAddress.Value =
            State.ParliamentContract.GetEmergencyResponseOrganizationAddress.Call(new Empty());

        return State.EmergencyResponseOrganizationAddress.Value;
    }

    public override Empty UpdateMultipleCandidateInformation(UpdateMultipleCandidateInformationInput input)
    {
        Assert(
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName) == Context.Sender,
            "Only consensus contract can update candidate information.");

        foreach (var updateCandidateInformationInput in input.Value)
            UpdateCandidateInformation(updateCandidateInformationInput);

        return new Empty();
    }

    public override Empty UpdateMinersCount(UpdateMinersCountInput input)
    {
        Context.LogDebug(() =>
            $"Consensus Contract Address: {Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName)}");
        Context.LogDebug(() => $"Sender Address: {Context.Sender}");
        Assert(
            Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName) == Context.Sender,
            "Only consensus contract can update miners count.");
        State.MinersCount.Value = input.MinersCount;
        SyncSubsidyInfoAfterReduceMiner();
        return new Empty();
    }

    public override Empty SetTreasurySchemeIds(SetTreasurySchemeIdsInput input)
    {
        Assert(State.TreasuryHash.Value == null, "Treasury profit ids already set.");
        State.TreasuryHash.Value = input.TreasuryHash;
        State.WelfareHash.Value = input.WelfareHash;
        State.SubsidyHash.Value = input.SubsidyHash;
        State.WelcomeHash.Value = input.WelcomeHash;
        State.FlexibleHash.Value = input.FlexibleHash;
        return new Empty();
    }

    public override Empty ReplaceCandidatePubkey(ReplaceCandidatePubkeyInput input)
    {
        Assert(IsCurrentCandidateOrInitialMiner(input.OldPubkey),
            "Pubkey is neither a current candidate nor an initial miner.");
        Assert(!IsPubkeyBanned(input.OldPubkey) && !IsPubkeyBanned(input.NewPubkey),
            "Pubkey is in already banned.");

        // Permission check.
        Assert(Context.Sender == GetCandidateAdmin(new StringValue { Value = input.OldPubkey }), "No permission.");

        // Record the replacement.
        PerformReplacement(input.OldPubkey, input.NewPubkey);

        var oldPubkeyBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(input.OldPubkey));
        var newPubkeyBytes = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(input.NewPubkey));

        //     Remove origin pubkey from Candidates, DataCentersRankingList and InitialMiners; then add new pubkey.
        var candidates = State.Candidates.Value;
        Assert(!candidates.Value.Contains(newPubkeyBytes), "New pubkey is already a candidate.");
        if (candidates.Value.Contains(oldPubkeyBytes))
        {
            candidates.Value.Remove(oldPubkeyBytes);
            candidates.Value.Add(newPubkeyBytes);
            State.Candidates.Value = candidates;
        }

        var rankingList = State.DataCentersRankingList.Value;
        //the profit receiver is not exist but candidate in the data center ranking list
        if (rankingList.DataCenters.ContainsKey(input.OldPubkey))
        {
            rankingList.DataCenters.Add(input.NewPubkey, rankingList.DataCenters[input.OldPubkey]);
            rankingList.DataCenters.Remove(input.OldPubkey);
            State.DataCentersRankingList.Value = rankingList;

            // Notify Profit Contract to update backup subsidy profiting item.
            if (State.ProfitContract.Value == null)
                State.ProfitContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            
            var oldProfitReceiver = GetProfitsReceiverOrDefault(input.OldPubkey);
            var profitReceiver = oldProfitReceiver.Value.Any()
                ? oldProfitReceiver
                : null;
            RemoveBeneficiary(input.OldPubkey);
            AddBeneficiary(input.NewPubkey, profitReceiver);
        }

        var initialMiners = State.InitialMiners.Value;
        if (initialMiners.Value.Contains(oldPubkeyBytes))
        {
            initialMiners.Value.Remove(oldPubkeyBytes);
            initialMiners.Value.Add(newPubkeyBytes);
            State.InitialMiners.Value = initialMiners;
        }

        //     For CandidateVotes and CandidateInformation, just replace value of origin pubkey.
        var candidateVotes = State.CandidateVotes[input.OldPubkey];
        if (candidateVotes != null)
        {
            candidateVotes.Pubkey = newPubkeyBytes;
            State.CandidateVotes[input.NewPubkey] = candidateVotes;
            State.CandidateVotes.Remove(input.OldPubkey);
        }

        var candidateInformation = State.CandidateInformationMap[input.OldPubkey];
        if (candidateInformation != null)
        {
            candidateInformation.Pubkey = input.NewPubkey;
            State.CandidateInformationMap[input.NewPubkey] = candidateInformation;
            State.CandidateInformationMap.Remove(input.OldPubkey);
        }

        //     Ban old pubkey.
        State.BannedPubkeyMap[input.OldPubkey] = true;

        ReplaceCandidateProfitsReceiver(input.OldPubkey, input.NewPubkey);
        
        Context.Fire(new CandidatePubkeyReplaced
        {
            OldPubkey = input.OldPubkey,
            NewPubkey = input.NewPubkey
        });

        return new Empty();
    }
    
    private void ReplaceCandidateProfitsReceiver(string oldPubkey, string newPubkey)
    {
        //Check profit receiver
        var beneficiary = GetProfitsReceiverOrDefault(oldPubkey);

        // Update profits receiver if needed.
        if (State.TreasuryContract.Value == null)
            State.TreasuryContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);

        if (beneficiary.Value.Any())
        {
            //remove profits receiver
            State.TreasuryContract.ReplaceCandidateProfitsReceiver.Send(new ReplaceCandidateProfitsReceiverInput
            {
                OldPubkey = oldPubkey,
                NewPubkey = newPubkey
            });
        }
    }

    private void PerformReplacement(string oldPubkey, string newPubkey)
    {
        State.CandidateReplacementMap[newPubkey] = oldPubkey;

        // Initial pubkey is:
        // - miner pubkey of the first round (aka. Initial Miner), or
        // - the pubkey announced election

        var initialPubkey = State.InitialPubkeyMap[oldPubkey] ?? oldPubkey;
        State.InitialPubkeyMap[newPubkey] = initialPubkey;

        State.InitialToNewestPubkeyMap[initialPubkey] = newPubkey;

        // Notify Consensus Contract to update replacement information. (Update from old record.)
        if (State.AEDPoSContract.Value == null)
            State.AEDPoSContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

        State.AEDPoSContract.RecordCandidateReplacement.Send(new RecordCandidateReplacementInput
        {
            OldPubkey = oldPubkey,
            NewPubkey = newPubkey
        });

        var oldPubkeyByteString = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(oldPubkey));
        // Notify Vote Contract to replace option if this is not the initial miner case.
        if (!State.InitialMiners.Value.Value.Contains(oldPubkeyByteString))
        {
            State.VoteContract.RemoveOption.Send(new RemoveOptionInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Option = oldPubkey
            });
            State.VoteContract.AddOption.Send(new AddOptionInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Option = newPubkey
            });
        }

        State.CandidateSponsorMap[newPubkey] = State.CandidateSponsorMap[oldPubkey];
        State.CandidateSponsorMap.Remove(oldPubkey);

        var managedPubkeys = State.ManagedCandidatePubkeysMap[Context.Sender];
        managedPubkeys.Value.Remove(oldPubkeyByteString);
        managedPubkeys.Value.Add(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(newPubkey)));
        State.ManagedCandidatePubkeysMap[Context.Sender] = managedPubkeys;

        Context.LogDebug(() => $"Pubkey replacement happened: {oldPubkey} -> {newPubkey}");
    }

    public override StringValue GetNewestPubkey(StringValue input)
    {
        return new StringValue { Value = GetNewestPubkey(input.Value) };
    }

    public override Empty RemoveEvilNode(StringValue input)
    {
        Assert(Context.Sender == GetEmergencyResponseOrganizationAddress(), "No permission.");
        var address = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(input.Value));
        Assert(
            State.Candidates.Value.Value.Select(p => p.ToHex()).Contains(input.Value) ||
            State.InitialMiners.Value.Value.Select(p => p.ToHex()).Contains(input.Value),
            "Cannot remove normal node.");
        Assert(!State.BannedPubkeyMap[input.Value], $"{input.Value} already banned.");
        UpdateCandidateInformation(new UpdateCandidateInformationInput
        {
            Pubkey = input.Value,
            IsEvilNode = true
        });
        return new Empty();
    }

    private string GetNewestPubkey(string pubkey)
    {
        var initialPubkey = State.InitialPubkeyMap[pubkey] ?? pubkey;
        return State.InitialToNewestPubkeyMap[initialPubkey] ?? initialPubkey;
    }

    private void SyncSubsidyInfoAfterReduceMiner()
    {
        var rankingList = State.DataCentersRankingList.Value;
        if (rankingList == null)
            return;
        var validDataCenterCount = GetValidationDataCenterCount();
        if (rankingList.DataCenters.Count <= validDataCenterCount) return;
        Context.LogDebug(() => "sync DataCenter after reduce bp");
        var diffCount = rankingList.DataCenters.Count.Sub(validDataCenterCount);
        var toRemoveList = rankingList.DataCenters.OrderBy(x => x.Value)
            .Take(diffCount).ToList();
        foreach (var kv in toRemoveList)
        {
            rankingList.DataCenters.Remove(kv.Key);
            RemoveBeneficiary(kv.Key);
        }

        State.DataCentersRankingList.Value = rankingList;
    }

    public override Empty SetProfitsReceiver(SetProfitsReceiverInput input)
    {
        Assert(
            Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName) == Context.Sender,
            "No permission.");
        var rankingList = State.DataCentersRankingList;
        if (!rankingList.Value.DataCenters.ContainsKey(input.CandidatePubkey)) return new Empty();
        var beneficiaryAddress = input.PreviousReceiverAddress.Value.Any()
            ? input.PreviousReceiverAddress
            : Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(input.CandidatePubkey));
        //set same profits receiver address
        if (beneficiaryAddress == input.ReceiverAddress)
        {
            return new Empty();
        }
        RemoveBeneficiary(input.CandidatePubkey,beneficiaryAddress);
        AddBeneficiary(input.CandidatePubkey,input.ReceiverAddress);

        return new Empty();
    }

    #region TakeSnapshot

    public override Empty TakeSnapshot(TakeElectionSnapshotInput input)
    {
        if (State.AEDPoSContract.Value == null)
            State.AEDPoSContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

        Assert(State.AEDPoSContract.Value == Context.Sender, "No permission.");

        SavePreviousTermInformation(input);

        if (State.ProfitContract.Value == null)
        {
            var profitContractAddress =
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            // Return if profit contract didn't deployed. (Often in test cases.)
            if (profitContractAddress == null) return new Empty();
            State.ProfitContract.Value = profitContractAddress;
        }

        // Update snapshot of corresponding voting record by the way.
        State.VoteContract.TakeSnapshot.Send(new TakeSnapshotInput
        {
            SnapshotNumber = input.TermNumber,
            VotingItemId = State.MinerElectionVotingItemId.Value
        });

        State.CurrentTermNumber.Value = input.TermNumber.Add(1);

        var previousTermMinerList =
            State.AEDPoSContract.GetPreviousTermMinerPubkeyList.Call(new Empty()).Pubkeys.ToList();

        foreach (var pubkey in previousTermMinerList)
            UpdateCandidateInformation(pubkey, input.TermNumber, previousTermMinerList);

        if (State.DividendPoolContract.Value == null)
            State.DividendPoolContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);

        var symbolList = State.DividendPoolContract.GetSymbolList.Call(new Empty());
        var amountsMap = symbolList.Value.ToDictionary(s => s, s => 0L);
        State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
        {
            SchemeId = State.SubsidyHash.Value,
            Period = input.TermNumber,
            AmountsMap = { amountsMap }
        });

        State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
        {
            SchemeId = State.WelfareHash.Value,
            Period = input.TermNumber,
            AmountsMap = { amountsMap }
        });

        return new Empty();
    }

    private void SavePreviousTermInformation(TakeElectionSnapshotInput input)
    {
        var snapshot = new TermSnapshot
        {
            MinedBlocks = input.MinedBlocks,
            EndRoundNumber = input.RoundNumber
        };

        if (State.Candidates.Value == null) return;

        foreach (var pubkey in State.Candidates.Value.Value)
        {
            var votes = State.CandidateVotes[pubkey.ToHex()];
            var validObtainedVotesAmount = 0L;
            if (votes != null) validObtainedVotesAmount = votes.ObtainedActiveVotedVotesAmount;

            snapshot.ElectionResult.Add(pubkey.ToHex(), validObtainedVotesAmount);
        }

        State.Snapshots[input.TermNumber] = snapshot;
    }

    private void UpdateCandidateInformation(string pubkey, long lastTermNumber,
        List<string> previousMiners)
    {
        var candidateInformation = State.CandidateInformationMap[pubkey];
        if (candidateInformation == null) return;
        candidateInformation.Terms.Add(lastTermNumber);
        var victories = GetVictories(previousMiners);
        candidateInformation.ContinualAppointmentCount = victories.Contains(ByteStringHelper.FromHexString(pubkey))
            ? candidateInformation.ContinualAppointmentCount.Add(1)
            : 0;
        State.CandidateInformationMap[pubkey] = candidateInformation;
    }

    #endregion
}