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
- How aelf system contracts use the Vote Contract to achieve node election
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
? Enter the required param <isQuadratic>: false
? Enter the required param <ticketCost>:
```

In this way, you have successfully created a voting event with options "A" and "B" 
that accepting users locking their ELF tokens for voting.

Run this command to get your `VotingItemId` for the voting event you just created:

```
aelf-command event -e ${endpoint} ${transactionId}
```

## How to vote for a voting event
