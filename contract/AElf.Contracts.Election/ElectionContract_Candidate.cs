using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election;

/// <summary>
///     AnnounceElection & QuitElection
/// </summary>
public partial class ElectionContract
{
    #region SetCandidateAdmin

    public override Empty SetCandidateAdmin(SetCandidateAdminInput input)
    {
        Assert(IsCurrentCandidateOrInitialMiner(input.Pubkey),
            "Pubkey is neither a current candidate nor an initial miner.");
        Assert(!IsPubkeyBanned(input.Pubkey), "Pubkey is already banned.");

        // Permission check
        var pubkey = State.InitialPubkeyMap[input.Pubkey] ?? input.Pubkey;
        if (Context.Sender != GetParliamentDefaultAddress())
        {
            if (State.CandidateAdmins[pubkey] == null)
            {
                // If admin is not set before (due to old contract code)
                Assert(Context.Sender == Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(input.Pubkey)),
                    "No permission.");
            }
            else
            {
                var oldCandidateAdmin = State.CandidateAdmins[pubkey];
                Assert(Context.Sender == oldCandidateAdmin, "No permission.");
            }
        }

        State.CandidateAdmins[pubkey] = input.Admin;

        var pubkeyByteString = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(pubkey));

        var newAdminManagedPubkeys = State.ManagedCandidatePubkeysMap[input.Admin] ?? new PubkeyList();
        if (!newAdminManagedPubkeys.Value.Contains(pubkeyByteString))
            newAdminManagedPubkeys.Value.Add(pubkeyByteString);
        State.ManagedCandidatePubkeysMap[input.Admin] = newAdminManagedPubkeys;

        var oldAdminManagedPubkeys = State.ManagedCandidatePubkeysMap[Context.Sender] ?? new PubkeyList();
        if (oldAdminManagedPubkeys.Value.Contains(pubkeyByteString))
            oldAdminManagedPubkeys.Value.Remove(pubkeyByteString);
        State.ManagedCandidatePubkeysMap[Context.Sender] = oldAdminManagedPubkeys;

        return new Empty();
    }

    #endregion

    private bool IsPubkeyBanned(string pubkey)
    {
        return State.BannedPubkeyMap[pubkey];
    }

    private Address GetParliamentDefaultAddress()
    {
        if (State.ParliamentContract.Value == null)
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);

        return State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
    }

    private bool IsCurrentCandidateOrInitialMiner(string pubkey)
    {
        var isCurrentCandidate = State.CandidateInformationMap[pubkey] != null &&
                                 State.CandidateInformationMap[pubkey].IsCurrentCandidate;
        var isInitialMiner = State.InitialMiners.Value.Value.Contains(
            ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(pubkey)));
        return isCurrentCandidate || isInitialMiner;
    }

    #region AnnounceElection

    /// <summary>
    ///     Actually this method is for adding an option of the Voting Item.
    ///     Thus the limitation of candidates will be limited by the capacity of voting options.
    ///     The input is candidate admin, better be an organization address of Association Contract.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty AnnounceElection(Address input)
    {
        var recoveredPublicKey = Context.RecoverPublicKey();
        AnnounceElection(recoveredPublicKey);

        var pubkey = recoveredPublicKey.ToHex();
        var address = Address.FromPublicKey(recoveredPublicKey);

        Assert(input.Value.Any(), "Admin is needed while announcing election.");
        Assert(State.ManagedCandidatePubkeysMap[address] == null, "Candidate cannot be others' admin.");
        State.CandidateAdmins[pubkey] = input;
        var managedPubkeys = State.ManagedCandidatePubkeysMap[input] ?? new PubkeyList();
        managedPubkeys.Value.Add(ByteString.CopyFrom(recoveredPublicKey));
        State.ManagedCandidatePubkeysMap[input] = managedPubkeys;

        LockCandidateNativeToken();

        AddCandidateAsOption(pubkey);

        if (State.Candidates.Value.Value.Count <= GetValidationDataCenterCount())
        {
            State.DataCentersRankingList.Value.DataCenters.Add(pubkey, 0);
            RegisterCandidateToSubsidyProfitScheme(pubkey);
        }

        return new Empty();
    }

    public override Empty AnnounceElectionFor(AnnounceElectionForInput input)
    {
        var pubkey = input.Pubkey;
        var pubkeyBytes = ByteArrayHelper.HexStringToByteArray(pubkey);
        var address = Address.FromPublicKey(pubkeyBytes);
        AnnounceElection(pubkeyBytes);
        var admin = input.Admin ?? Context.Sender;
        State.CandidateAdmins[pubkey] = admin;
        var managedPubkeys = State.ManagedCandidatePubkeysMap[admin] ?? new PubkeyList();
        managedPubkeys.Value.Add(ByteString.CopyFrom(pubkeyBytes));
        State.ManagedCandidatePubkeysMap[admin] = managedPubkeys;
        LockCandidateNativeToken();
        AddCandidateAsOption(pubkey);
        if (State.Candidates.Value.Value.Count <= GetValidationDataCenterCount())
        {
            State.DataCentersRankingList.Value.DataCenters.Add(pubkey, 0);
            RegisterCandidateToSubsidyProfitScheme(pubkey);
        }

        State.CandidateSponsorMap[input.Pubkey] = Context.Sender;
        return new Empty();
    }

    private void AnnounceElection(byte[] pubkeyBytes)
    {
        var pubkey = pubkeyBytes.ToHex();
        var pubkeyByteString = ByteString.CopyFrom(pubkeyBytes);

        Assert(!State.InitialMiners.Value.Value.Contains(pubkeyByteString),
            "Initial miner cannot announce election.");

        var candidateInformation = State.CandidateInformationMap[pubkey];

        if (candidateInformation != null)
        {
            Assert(!candidateInformation.IsCurrentCandidate,
                $"This public key already announced election. {pubkey}");
            candidateInformation.AnnouncementTransactionId = Context.OriginTransactionId;
            candidateInformation.IsCurrentCandidate = true;
            // In this way we can keep history of current candidate, like terms, missed time slots, etc.
            State.CandidateInformationMap[pubkey] = candidateInformation;
        }
        else
        {
            Assert(!IsPubkeyBanned(pubkey), "This candidate already banned before.");
            State.CandidateInformationMap[pubkey] = new CandidateInformation
            {
                Pubkey = pubkey,
                AnnouncementTransactionId = Context.OriginTransactionId,
                IsCurrentCandidate = true
            };
        }

        State.Candidates.Value.Value.Add(pubkeyByteString);
    }

    private void LockCandidateNativeToken()
    {
        if (State.TokenContract.Value == null)
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

        // Lock the token from sender for deposit of announce election
        var lockId = Context.OriginTransactionId;
        var lockVirtualAddress = Context.ConvertVirtualAddressToContractAddress(lockId);
        var sponsorAddress = Context.Sender;
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = sponsorAddress,
            To = lockVirtualAddress,
            Symbol = Context.Variables.NativeSymbol,
            Amount = ElectionContractConstants.LockTokenForElection,
            Memo = "Lock for announcing election."
        });
    }

    private void AddCandidateAsOption(string publicKey)
    {
        if (State.VoteContract.Value == null)
            State.VoteContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName);

        // Add this candidate as an option for the the Voting Item.
        State.VoteContract.AddOption.Send(new AddOptionInput
        {
            VotingItemId = State.MinerElectionVotingItemId.Value,
            Option = publicKey
        });
    }

    private void RegisterCandidateToSubsidyProfitScheme(string candidatePubkey)
    {
        if (State.ProfitContract.Value == null)
            State.ProfitContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);

        AddBeneficiary(candidatePubkey);
    }

    #endregion

    #region QuitElection

    /// <summary>
    ///     delete a option of voting,then sub the Shares from the corresponding ProfitItem
    /// </summary>
    /// <param name="input">Empty</param>
    /// <returns></returns>
    public override Empty QuitElection(StringValue input)
    {
        var pubkeyBytes = ByteArrayHelper.HexStringToByteArray(input.Value);
        QuitElection(pubkeyBytes);
        var pubkey = input.Value;

        var initialPubkey = State.InitialPubkeyMap[pubkey] ?? pubkey;
        Assert(Context.Sender == State.CandidateAdmins[initialPubkey], "Only admin can quit election.");
        var candidateInformation = State.CandidateInformationMap[pubkey];

        // Unlock candidate's native token.
        var lockId = candidateInformation.AnnouncementTransactionId;
        var lockVirtualAddress = Context.ConvertVirtualAddressToContractAddress(lockId);
        State.TokenContract.TransferFrom.Send(new TransferFromInput
        {
            From = lockVirtualAddress,
            To = State.CandidateSponsorMap[input.Value] ?? Address.FromPublicKey(pubkeyBytes),
            Symbol = Context.Variables.NativeSymbol,
            Amount = ElectionContractConstants.LockTokenForElection,
            Memo = "Quit election."
        });

        // Update candidate information.
        candidateInformation.IsCurrentCandidate = false;
        candidateInformation.AnnouncementTransactionId = Hash.Empty;
        State.CandidateInformationMap[pubkey] = candidateInformation;

        // Remove candidate public key from the Voting Item options.
        State.VoteContract.RemoveOption.Send(new RemoveOptionInput
        {
            VotingItemId = State.MinerElectionVotingItemId.Value,
            Option = pubkey
        });
        var dataCenterList = State.DataCentersRankingList.Value;
        if (dataCenterList.DataCenters.ContainsKey(pubkey))
        {
            dataCenterList.DataCenters[pubkey] = 0;
            UpdateDataCenterAfterMemberVoteAmountChanged(dataCenterList, pubkey, true);
            State.DataCentersRankingList.Value = dataCenterList;
        }

        var managedCandidatePubkey = State.ManagedCandidatePubkeysMap[Context.Sender];
        managedCandidatePubkey.Value.Remove(ByteString.CopyFrom(pubkeyBytes));
        if (managedCandidatePubkey.Value.Any())
            State.ManagedCandidatePubkeysMap[Context.Sender] = managedCandidatePubkey;
        else
            State.ManagedCandidatePubkeysMap.Remove(Context.Sender);

        State.CandidateSponsorMap.Remove(pubkey);

        return new Empty();
    }

    private void QuitElection(byte[] recoveredPublicKey)
    {
        var publicKeyByteString = ByteString.CopyFrom(recoveredPublicKey);

        Assert(State.Candidates.Value.Value.Contains(publicKeyByteString), "Target is not a candidate.");

        if (State.AEDPoSContract.Value == null)
            State.AEDPoSContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

        if (State.AEDPoSContract.Value != null)
            Assert(
                !State.AEDPoSContract.GetCurrentMinerList.Call(new Empty()).Pubkeys
                    .Contains(publicKeyByteString),
                "Current miners cannot quit election.");

        State.Candidates.Value.Value.Remove(publicKeyByteString);
    }

    #endregion
}