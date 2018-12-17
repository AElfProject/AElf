# DPoS Process

## 

# Voting / Election System

## Data Structure

```Protobuf
message Candidates {
    repeated Address Nodes = 1;
}

message Tickets {
    uint64 TotalTickets = 1;
    uint64 ExpiredTickets = 2;
    repeated VotingRecord VotingRecords = 3;
}

message VotingRecord {
    Address From = 1;
    Address To = 2;
    uint64 Count = 3;
    uint64 RoundNumber = 4;
    Hash TransactionId = 5;
    google.protobuf.Timestamp VoteTimestamp = 6;
    google.protobuf.Timestamp LockTime = 7;
}

message ElectionSnapshot {
    uint64 EndRoundNumber = 1;// The key of DividendsMap and DPoSInfoMap
    uint64 BlocksCount = 2;
    repeated MinerSnapshot MinersSnapshot = 3;
}

message MinerSnapshot {
    Address MinerAddress = 1;
    uint64 VotersWeights = 2;
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