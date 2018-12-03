# Designs of Election System

## Data Structure

```Protobuf
message Candidates {
    repeated Address Nodes = 1;
}

message Tickets {
    uint64 RemainingTickets = 1;
    repeated VotingRecord VotingRecord = 2;
}

message VotingRecord {
    Address From = 1;
    Address To = 2;
    uint64 TicketsCount = 3;
    uint64 RoundNumber = 4;// Round number of the voting behavior
    Hash TransactionId = 5;// Related transaction id
    bool State = 6;// Regreted or not
}

message ElectionSnapshot {
    uint64 StartRoundNumber = 1;
    uint64 EndRoundNumber = 2;
    uint64 Blocks = 3;
    repeated TicketsMap TicketsMap = 4;
}

message TicketsMap {
    Address CandidateAddress = 1;
    uint64 TicketsCount = 2;
    uint64 TotalWeights = 3;
}

```

In `TokenContract`: 

- A field of `Candidates` (called `CandidatesField`)
  - simply record all the candidates (who announuced election).

In `ConsensusContract`:

- A field of `Candidates` (called `CandidatesField`)
  - record all the candidates, and this field should only be updated by the calling of `AnnouceElection` from `TokenContract`.

- A map of `Address` to `Tickets` (called `BalanceMap`) 
  - maintains all the tickets of voters, including their voting histories.

- A map of `UInt64Value` to `ElectionSnapshot` (called `SnapshotMap`)
  - maintains the snapshots of every replacement of block producers.

## For candidates

### Announce election
Send transaction `AnnouceElection` to `TokenContract`;
- No parameter

### Quit election
Send transaction `QuitElection` to `ConsensusContract`
- No parameter

## For voters

### Vote
Send transaction `Vote` to `TokenContract`
- `Address` CandidateAddress
- `ulong` amount

### Regret
Send transaction `Regret` to `ConsensusContract`
- `Address` CandidateAddress
- `ulong` amount

### Withdraw (to get ELFs back)
Send transaction `Withdraw` to `ConsensusContract`
- `Address` CandidateAddress
- `ulong` amount

or 

- `Hash` TransactionId
