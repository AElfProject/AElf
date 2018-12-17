# Voting / Election System

## Available Methods

### AnnounceElection

*No params*

To announce election.

This will currently lock **100,000** elf tokens of the caller to Consensus Contract Account.

At the same time, the caller's public key will be added to the candidates list.

### QuitElection

*No params*

To quit election.

Transfer the specific amount of tokens back to the caller, at the same time remove related public key from the candidates list.

### Vote

Params:
- string publicKeyHexString
- ulong amount
- ink lockdays

To vote to a candidate via specifc amount of tickets and lock days.

This behavior will generate a instance of VotingReocrd, which will append to both the voter and candidate.

The **Weight** of this voting (VotingRecord) will be calculated by tickets amount and lock days.

In addition, current candidates can't vote to any public key.

### GetAllDividends

*No params*

To get all the dividends for locked tokens of the caller, which should be a voter.

### WithdrawAll

*No params*

To withdraw all the locked tokens of the caller.

### IsCandidate

Params:
- string publicKeyHexString

To check whether the provided public key contained by the candidates list.

### GetCandidatesList

*No params*

To get current candidates list.

### GetCandidateHistoryInfo

Params:
- string publicKeyHexString

To get the history information of provided candidate.

No need to be the current candidate.

### GetCurrentMiners

*No params*

To get current miners.

### GetTicketsInfo

Params:
- string publicKeyHexString

To get the tickets information of provided public key.

If this public key ever joined election, the voting records will also contain his supportters'.

### GetBlockchainAge

*No params*

To get the age of this blockchain. (Currently the unit is day.)

### GetCurrentVictories

*No params*

To get the victories of ongoing election.

### GetTermSnapshot

Params:
- ulong termNumbner

To get the term snapshot of provided term number.

### GetTermNumberByRoundNumber

Params:
- ulong roundNumber

To get the term number of provided round number.

## Data Structure

```Protobuf

message Miners {
    uint64 TermNumber = 1;
    repeated string PublicKeys = 2;
}

message TermNumberLookUp {
    map<uint64, uint64> Map = 1;// Term number -> Round number.
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

# 投票/选举系统

## 可用方法

### AnnounceElection

*无参数*

用于参加下一届选举。

目前该方法会为调用者锁**100,000**个ELF，转入共识合约的账户。

同时，调用者的公钥会被加入候选人列表中。

### QuitElection

*无参数*

用于放弃下一届及以后的选举。

调用者将会拿回参加竞选时锁仓的代币，其公钥也会被移出候选人列表。

### Vote

Params:
- string publicKeyHexString
- ulong amount
- ink lockamount

为候选人投票，所需参数为投票数目amount，锁仓时间lockamount。

该方法会为投票人和被投票的候选人共同增加一个VotingReocrd实例。

这一笔投票的**权重**与投票数目、锁仓时间都相关。

当前候选人不能为任何人投票。

### GetAllDividends

*无参数*

投票者获取自己所有的锁仓分红。

### WithdrawAll

*无参数*

投票者赎回自己的选票。

### IsCandidate

Params:
- string publicKeyHexString

检查提供的公钥是否是候选人的公钥。

### GetCandidatesList

*无参数*

获取候选人公钥列表。

### GetCandidateHistoryInfo

Params:
- string publicKeyHexString

获取所提供公钥的候选人的贡献历史。

该候选人不必是当前候选人。

### GetCurrentMiners

*无参数*

获取当前在任的区块生产者公钥列表，

### GetTicketsInfo

Params:
- string publicKeyHexString

获取所提供公钥的投票详情，

如果该公钥曾经参与过竞选，其投票详情中会包括其支持者对他的投票。

### GetBlockchainAge

*无参数*

获取区块链的年龄。（当前时间为天）。

### GetCurrentVictories

*无参数*

获取当前竞选的前17名。

### GetTermSnapshot

Params:
- ulong termNumbner

获取所提供届数的快照。

### GetTermNumberByRoundNumber

Params:
- ulong roundNumber

获取所提供轮数所在的届数。