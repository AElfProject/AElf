# Voting / Election System

## Data Structure

```Protobuf

message Miners {
    uint64 TermNumber = 1;
    repeated string PublicKeys = 2;
}

message Candidates {
    repeated string PublicKeys = 1;
}

message Tickets {
    repeated VotingRecord VotingRecords = 1;
    uint64 ExpiredTickets = 2;
    uint64 TotalTickets = 3;
}

message VotingRecord {
    string From = 1;
    string To = 2;
    uint64 Count = 3;
    uint64 RoundNumber = 4;
    Hash TransactionId = 5;
    google.protobuf.Timestamp VoteTimestamp = 6;
    repeated uint32 LockDaysList = 7;// Can be renewed by adding items.
    uint64 UnlockAge = 8;
    uint64 TermNumber = 9;
}

message TermSnapshot {
    uint64 EndRoundNumber = 1;
    uint64 TotalBlocks = 2;
    repeated CandidateInTerm CandidatesSnapshot = 3;
    uint64 TermNumber = 4;
}

message Round {
    uint64 RoundNumber = 1;
    map<string, MinerInRound> RealTimeMinersInfo = 2;
    int32 MiningInterval = 3;
}

message CandidateInTerm {
    string PublicKey = 1;
    uint64 Votes = 2;
}

message MinerInRound {
    int32 Order = 1;
    bool IsExtraBlockProducer = 2;
    Hash InValue = 3;
    Hash OutValue = 4;
    Hash Signature = 5;
    google.protobuf.Timestamp ExpectedMiningTime = 6;
    uint64 ProducedBlocks = 7;
    bool IsForked = 8;
    uint64 MissedTimeSlots = 9;
    uint64 RoundNumber = 10;
    string PublicKey = 11;
    uint64 PackagedTxsCount = 12;
}

message CandidateInHistory {
    repeated uint64 Terms = 1;
    uint64 ProducedBlocks = 2;
    uint64 MissedTimeSlots = 3;
    uint64 ContinualAppointmentCount = 4;
    uint64 ReappointmentCount = 5;
}
```

In `ConsensusContract`:

- A field of `Candidates` (called `CandidatesField`)
  - record all the candidates.

- A map of `BytesValue` to `StringValue` (called `AliasMap`)
  - maintains all the aliases of candidates.

- A map of `BytesValue` to `Tickets` (called `BalanceMap`)
  - maintains all the tickets of voters, including their voting histories.

- A map of `UInt64Value` to `ElectionSnapshot` (called `SnapshotMap`)
  - maintains the snapshots of every replacement of block producers; key stands for round number.
  
## For candidates

### Announce election
Send transaction `AnnouceElection` to `ConsensusContract`;
- No parameter

### Quit election
Send transaction `QuitElection` to `ConsensusContract`
- No parameter

## For voters

### Vote
Send transaction `Vote` to `ConsensusContract`
- `byte[]` candidatePubKey
- `ulong` ticketsAmount
- `int` lockDays (default and minimum value is 90)

### Renew
Send transaction `Renew` to `ConsensusContract`
- `byte[]` candidatePubKey
- `ulong` ticketsAmount
- `int` lockDays (default and minimum value is 90)

### Withdraw (to get ELFs back)
Send transaction `Withdraw` to `ConsensusContract`
- `Address` candidateAddress
- `ulong` ticketsAmount

or 

- `Hash` transactionId

### GetDividends
- No parameter

### QueryDividends
- No parameter