# Vote Contract

## Overview

Vote Contract can be used to manage the lifecycle of a voting event.

When the aelf blockchain equipped with DPoS Consensus is launched, the Vote Contract is used as one of the underlying contracts for node election.
In this scenario, the candidate's public key will be registered as an option for a voting event, 
and users can increase the number of votes for their supported nodes by locking ELF tokens.

Users of aelf blockchain can quickly create their own voting events through the Vote Contract without implementing and deploying a voting contract themselves: 
they only need to pay a tiny amount of transaction fees.

In this article, we will discuss:

- How to create a voting event
- How to vote for a voting event
- Application of Vote Contract in AElf Node Election
- Vote Contract method explanation

## How to create a voting event

In the Vote Contract, the `VotingItem` structure is used to store basic information about a voting event:

```
message VotingItem {
    // The voting activity id.
    aelf.Hash voting_item_id = 1;
    // The token symbol which will be accepted.
    string accepted_currency = 2;
    // Whether the vote will lock token.
    bool is_lock_token = 3;
    // The current snapshot number.
    int64 current_snapshot_number = 4;
    // The total snapshot number.
    int64 total_snapshot_number = 5;
    // The list of options.
    repeated string options = 6;
    // The register time of the voting activity.
    google.protobuf.Timestamp register_timestamp = 7;
    // The start time of the voting.
    google.protobuf.Timestamp start_timestamp = 8;
    // The end time of the voting.
    google.protobuf.Timestamp end_timestamp = 9;
    // The start time of current round of the voting.
    google.protobuf.Timestamp current_snapshot_start_timestamp = 10;
    // The sponsor address of the voting activity.
    aelf.Address sponsor = 11;
}
```

To create a new voting event, users can send a `Register` transaction to Vote Contract.

The input type of `Register` method is:

```
message VotingRegisterInput {
    // The start time of the voting.
    google.protobuf.Timestamp start_timestamp = 1;
    // The end time of the voting.
    google.protobuf.Timestamp end_timestamp = 2;
    // The token symbol which will be accepted.
    string accepted_currency = 3;
    // Whether the vote will lock token.
    bool is_lock_token = 4;
    // The total number of snapshots of the vote.
    int64 total_snapshot_number = 5;
    // The list of options.
    repeated string options = 6;
}
```

If you're using the **aelf-command** tool:

```
aelf-command send AElf.ContractNames.Vote Register
? Enter the required param <startTimestamp>: 2023/10/10 13:30
? Enter the required param <endTimestamp>: 2023/10/10 14:00
? Enter the required param <acceptedCurrency>: ELF
? Enter the required param <isLockToken>: true
? Enter the required param <totalSnapshotNumber>: 5
? Enter the required param <options>: ["A","B"]
```

In this way, you have successfully created a voting event with options "A" and "B" 
that accepting users locking their ELF tokens for voting.

Run this command to get your `VotingItemId` for the voting event you just created 
from a `VotingItemRegistered` event:

```
aelf-command event -e ${endpoint} ${transactionId}
```

From now on, as the sponsor of this voting event, 
you can use the `TakeSnapshot` method at any time to save a snapshot of the current vote count for each option.

The input type of `TaskSnapshot` method is:

```
message TakeSnapshotInput {
    // The voting activity id.
    aelf.Hash voting_item_id = 1;
    // The snapshot number to take.
    int64 snapshot_number = 2;
}
```

If you're using the **aelf-command** tool:

```
aelf-command send AElf.ContractNames.Vote TakeSnapshot '{"voteItemId": "${vote_item_id}", "snapshot_number": 1}'
```

## How to vote for a voting event

### Vote

Users can use `Vote` method to vote to an option of a certain voting event.

The input type of `Vote` method is:

```
message VoteInput {
    // The voting activity id.
    aelf.Hash voting_item_id = 1;
    // The address of voter.
    aelf.Address voter = 2;
    // The amount of vote.
    int64 amount = 3;
    // The option to vote.
    string option = 4;
    // The vote id.
    aelf.Hash vote_id = 5;
    // Whether vote others.
    bool is_change_target = 6;
}
```

Among them, the fields `voter` and `vote_id` can be empty if you want to send the `Vote` transaction directly to the Vote Contract.
The `voter` will be the sender by default, and the `vote_id` can only be provided if you want to change your voting target(option).

If you're using the **aelf-command** tool, 
and it's the first time you're voting for this voting event:

```
aelf-command send AElf.ContractNames.Vote Vote
? Enter the required param <votingItemId>: ${voting_item_id}
? Enter the required param <voter>: 
? Enter the required param <amount>: 5
? Enter the required param <option>: A
? Enter the required param <voteId>: 
? Enter the required param <isChangeTarget>: 
```

If your voting amount is 5, then after this transaction is successfully executed,
your 5 ELF tokens will be locked to a virtual address
which is computed by:
- Voter's Address
- Vote Contract Address
- VotingItem Id

This is because the **VotingItem**'s `is_lock_token` filed is **true** when creating this voting event.

Run this command to get your `VoteId` from a `Voted` event.

```
aelf-command event -e ${endpoint} ${transactionId}
```

Even your voting action is completed, 
you can still modify the option you have voted for before withdrawal by sending a `Vote` transaction again.

```
aelf-command send AElf.ContractNames.Vote Vote
? Enter the required param <votingItemId>: ${voting_item_id}
? Enter the required param <voter>: 
? Enter the required param <amount>: 
? Enter the required param <option>: B
? Enter the required param <voteId>: ${vote_id}
? Enter the required param <isChangeTarget>: true
```

### Withdraw

If you have participated in a voting event that will lock tokens, 
you can redeem these tokens via `Withdraw` method at any time.
At the same time, your vote will also be withdrawn.

The input type of `Withdraw` method is merely a hash value:

```
message WithdrawInput {
    // The vote id.
    aelf.Hash vote_id = 1;
}
```

If you're using the **aelf-command** tool:

```
aelf-command send AElf.ContractNames.Vote Create '{"voteId": "${vote_id}"}'
```

By the way, after voting for the miner candidates of aelf, 
user's locked ELF tokens is not redeemable until the specified lock-up period ends,
this logic is implemented by the Election Contract.
In fact, when voting for nodes, 
ELF token locking is done by the Election Contract.

## Application of Vote Contract in AElf Node Election

Overall, the Election Contract creates a voting event during initialization by calling the `Register` method of the Vote Contract.

Afterward, although voters vote for nodes by sending a `Vote` transaction to the Election Contract, 
in reality, the counting of votes occurs in the Vote Contract.

The following is the code implementation for creating a voting event in the Election Contract:

```
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
```

This method will be executed in the Genesis Block during the launching of aelf blockchain.

After the voting event is created, 
the candidate can declare its participation in the election by pledging an ELF token, sending a `AnnounceElection` method. 
At this point, the Election Contract will add a voting option to the voting event responsible for the node election.
And the option is the public key of the candidate.

```
public override Empty AnnounceElection(Address input)
{
    var recoveredPublicKey = Context.RecoverPublicKey();
    AnnounceElection(recoveredPublicKey);

    var pubkey = recoveredPublicKey.ToHex();
    var address = Address.FromPublicKey(recoveredPublicKey);

    // ...

    LockCandidateNativeToken();

    AddCandidateAsOption(pubkey);
    
    // ...

    return new Empty();
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
```

When a user sends a `Vote` transaction of the Election contract for voting, 
the token locking step is completed through the `Lock` method of the [MultiToken Contract](https://aelf-ean.readthedocs.io/en/latest/reference/smart-contract/multi-token-contract.html#lock-unlock), 
and the voting itself is completed through the `Vote` method of the Vote Contract.

```
public override Hash Vote(VoteMinerInput input)
{
    // ...

    LockTokensOfVoter(input.Amount, voteId);
    TransferTokensToVoter(input.Amount);
    CallVoteContractVote(input.Amount, input.CandidatePubkey, voteId);
    AddBeneficiaryToVoter(GetVotesWeight(input.Amount, lockSeconds), lockSeconds, voteId);

    // Handle ranking list
    // ...

    return voteId;
}

private void LockTokensOfVoter(long amount, Hash voteId)
{
    State.TokenContract.Lock.Send(new LockInput
    {
        Address = Context.Sender,
        Symbol = Context.Variables.NativeSymbol,
        LockId = voteId,
        Amount = amount,
        Usage = "Voting for Main Chain Miner Election."
    });
}

private void CallVoteContractVote(long amount, string candidatePubkey, Hash voteId)
{
    State.VoteContract.Vote.Send(new VoteInput
    {
        Voter = Context.Sender,
        VotingItemId = State.MinerElectionVotingItemId.Value,
        Amount = amount,
        Option = candidatePubkey,
        VoteId = voteId
    });
}
```

## Vote Contract method explanation

### Register

This method essentially does two things:

- Initializes a VotingItem instance and inserts it into State.VotingItems.
- Initializes a VotingResult instance and inserts it into State.VotingResults.

The call to `State.TokenContract.IsInWhiteList` is intended to ensure that when `input.AcceptedCurrency` is created, 
the Vote Contract is added to the whitelist. 
This allows the Vote contract to lock a specified amount of tokens through the MultiToken Contract's Lock method without requiring user consent (refer to related terms in the MultiToken contract explanation).

### Vote

In addition to adding a VotingRecord instance to StateDb, the voting action also:

- Modifies the corresponding voting project's current VotingResult.
- Adds a VoteId record for the user in `State.VotedItemsMap`.

### Withdraw

Redemption is a mirrored operation of Vote, but regarding operation permissions, it depends on the IsLockToken field of the voting project:

- If IsLockToken is true, it means that the token locking for this vote is handled by the Vote contract. In this case, check whether the sender is the voter.
- If IsLockToken is false, it means that the token locking for this vote is handled by the sponsor of the voting project. In this case, check whether the sender is the sponsor."

### TakeSnapshot

This operation involves three actions:

1. Fetches the current voting result and stores it as the previous result.
2. Increments the SnapshotNumber of the corresponding VotingItem in State.VotingItems.
3. Initializes the voting result for the next period.

### AddOption / AddOptions / RemoveOption / RemoveOptions

Checks if the caller is the sponsor and then operates on the Option field of the corresponding VotingItem in `State.VotingItems`.