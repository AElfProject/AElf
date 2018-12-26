# Voting / Election System

## Available Methods

- All the methods without parameter should a `string` type of parameter temporarily due to a bug in signature.

### `AnnounceElection`

Params:
- `string` alias

To announce election.

This will currently lock **100,000** elf tokens of the caller to Consensus Contract Account.

At the same time, the caller's public key will be added to the candidates list.

### `QuitElection`

*No params*

To quit election.

Transfer the specific amount of tokens back to the caller, at the same time remove related public key from the candidates list.

### `Vote`

Params:
- `string` publicKeyHexString
- `ulong` ticketsAmount
- `int` lockTime

To vote to a candidate via specifc amount of tickets and lock days.

This behavior will generate a instance of VotingReocrd, which will append to both the voter and candidate.

The **Weight** of this voting (VotingRecord) will be calculated by tickets amount and lock days.

In addition, current candidates can't vote to any public key.

### `ReceiveAllDividends`

*No params*

To get all the dividends for locked tokens of the caller, which should be a voter. The Dividends Contract will directly trasfer tokens to the caller.

### `WithdrawAll`

Params:
- `bool` withoutLimitation

To withdraw all the locked tokens of the caller.

### `WithdrawByDetail`

Params:
- `string` publicKeyHexString
- `ulong` ticketsAmount
- `int` lockTime

### `WithdrawByTransactionId`

Params:
- `Hash` transactionId

### `IsCandidate`

Params:
- `string` publicKeyHexString

Result Type:
`bool`

To check whether the provided public key contained by the candidates list.

### `GetCandidatesList` / `GetCandidatesListToFriendlyString`

*No params*

Result Type:
- `StringList` / `string`

To get current candidates list.

### `GetCandidateHistoryInfo` / `GetCandidateHistoryInfoToFriendlyString`

Params:
- `string` publicKeyHexString

Result Type:
`CandidateInHistory` / `string`

To get the history information of provided candidate.

No need to be the current candidate.

### `GetCurrentMiners` / `GetCurrentMinersToFriendlyString`

*No params*

Result Type:
- `StringList` / `string`

To get current miners.

### `GetTicketsInfo` / `GetTicketsInfoToFriendlyString`

Params:
- `string` publicKeyHexString

Result type:
- `Tickets` / `string`

To get the tickets information of provided public key.

If this public key ever joined election, the voting records will also contain his supportters'.

### `GetBlockchainAge`

*No params*

Result Type:
- `ulong`

To get the age of this blockchain. (Currently the unit is day.)

### `GetCurrentVictories` / `GetCurrentVictoriesToFriendlyString`

*No params*

Result Type:
- `StringList` / `string`

To get the victories of ongoing election.

### `GetTermSnapshot` / `GetTermSnapshotToFriendlyString`

Params:
- `ulong` termNumbner

Result Type:
- `TermSnapshot` / `string`

To get the term snapshot of provided term number.

### `GetTermNumberByRoundNumber`

Params:
- `ulong` roundNumber

Result Type:
- `ulong`

To get the term number of provided round number.

### `GetCurrentElectionInfo` / `GetCurrentElectionInfoToFriendlyString`

Params:
- `int` startIndex
- `int` length // If the length is 0, will return all results from startIndex.
- `int` orderBy // Default 0, which stands for order by announcement order.

Result Type:
- `TicketsDictionary` / `string`

To get the election information during the election.

orderBy:
0 - Announcement order. (Default)
1 - Tickets count ascending.
2 - Tickets count descending.

### `GetVotesCount`

*No params*

Result Type:
`ulong`

To get the total voting records of this system.

### `GetTicketsCount`

*No params*

Result Type:
`ulong`

To get the total tickets of this system (both valid and invalid).

### `QueryCurrentDividendsForVoters`

*No params*

Result Type:
`ulong`

To query dividends of current term for voters.

### `QueryCurrentDividends`

*No params*

Result Type:
`ulong`

To query total dividends of current term.

### `QueryAliasesInUse` / `QueryAliasesInUseToFriendlyString`

*No params*

Result Type:
`StringList` / `string`

To query all the alias in use.

### `QueryMinedBlockCountInCurrentTerm`

Params:
- `string` publicKeyHexString

Return Type:
`ulong`

To query the count of mined blocks by provided miner in current term.

### `InitialBalance`

Params:
- `Address` address
- `ulong` amount

Initial balance for the provided account.

Can only called by one of the initial miners.

# 投票/选举系统

## 可用方法

### `AnnounceElection`

- 由于一个签名相关的bug，没有参数的方法需要暂时加上一个string类型的参数。

参数:
- `string` alias

用于参加下一届选举。

目前该方法会为调用者锁**100,000**个ELF，转入共识合约的账户。

同时，调用者的公钥会被加入候选人列表中。

### `QuitElection`

*无参数*

用于放弃下一届及以后的选举。

调用者将会拿回参加竞选时锁仓的代币，其公钥也会被移出候选人列表。

### `Vote`

参数:
- `string` publicKeyHexString
- `ulong` amount
- `int` lockTime

为候选人投票，所需参数为投票数目amount，锁仓时间lockTime。

该方法会为投票人和被投票的候选人共同增加一个VotingReocrd实例。

这一笔投票的**权重**与投票数目、锁仓时间都相关。

当前候选人不能为任何人投票。

### `ReceiveAllDividends`

*无参数*

投票者获取自己所有的锁仓分红，调用后分红合约账户会直接进行转账。

### `WithdrawAll`

参数:
- `bool` withoutLimitation

投票者赎回自己的选票。

### `WithdrawByDetail`

参数:
- string publicKeyHexString
- ulong ticketsAmount
- int lockTime

### `WithdrawByTransactionId`

参数:
- Hash transactionId

### `IsCandidate`

参数:
- `string` publicKeyHexString

返回类型：
- `bool`

检查提供的公钥是否是候选人的公钥。

### `GetCandidatesList` / `GetCandidatesListToFriendlyString`

*无参数*

返回类型：
- `StringList` / `string`

获取候选人公钥列表。

### `GetCandidateHistoryInfo` / `GetCandidateHistoryInfoToFriendlyString`

参数:
- `string` publicKeyHexString

返回类型：
`CandidateInHistory` / `string`

获取所提供公钥的候选人的对区块链的贡献历史，如历史出块数量、错过时间槽数量等。

该候选人不必是当前候选人。

### `GetCurrentMiners` / `GetCurrentMinersToFriendlyString`

*无参数*

返回类型：
- `StringList` / `string`

获取当前在任的区块生产者公钥列表，

### `GetTicketsInfo` / `GetTicketsInfoToFriendlyString`

参数:
- `string` publicKeyHexString

返回类型：
- `Tickets` / `string`

获取所提供公钥的投票详情，

如果该公钥曾经参与过竞选，其投票详情中会包括其支持者对他的投票。

### `GetBlockchainAge`

*无参数*

返回类型：
- `ulong`

获取区块链的年龄。（当前单位为天）。

### `GetCurrentVictories` / `GetCurrentVictoriesToFriendlyString`

*无参数*

返回类型：
- `StringList` / `string`

获取当前竞选的前N名。

### `GetTermSnapshot` / `GetTermSnapshotToFriendlyString`

参数:
- `ulong` termNumbner

返回类型：
- `TermSnapshot` / `string`

获取所提供届数的快照。

### `GetTermNumberByRoundNumber`

参数:
- `ulong` roundNumber

返回类型：
- `ulong`

获取所提供轮数所在的届数。

### `GetCurrentElectionInfo` / `GetCurrentElectionInfoToFriendlyString`

参数:
- `int` startIndex
- `int` length // 如果length为0，会返回startIndex之后所有结果
- `int` orderBy // 默认为0，按参加选举顺序排序

返回类型:
- `TicketsDictionary` / `string`

竞选过程中获取所有候选人的选票详情。

orderBy:
0 - 参加竞选时间。（默认）
1 - 选票数升序。
2 - 选票数降序。

### `GetVotesCount`

*无参数*

返回类型:
`ulong`

获取当前系统投票的总次数。

### `GetTicketsCount`

*无参数*

返回类型:
`ulong`

获取当前系统投票的总票数。

### `QueryCurrentDividendsForVoters`

*无参数*

返回类型:
`ulong`

获取当前届给投票者的出块奖励分红总数。

### `QueryCurrentDividends`

*无参数*

返回类型:
`ulong`

获取当前届的出块奖励分红总数。

出块奖励分红取决于本届的出块数，会不断增加。

### `QueryAliasesInUse` / `QueryAliasesInUseToFriendlyString`

*无参数*

返回类型：
`StringList` / `string`

查询已经被使用的别名。

### `QueryMinedBlockCountInCurrentTerm`

参数:
- `string` publicKeyHexString

返回类型:
`ulong`

获取某节点当前届的出块数量。

### `InitialBalance`

参数:
- `Address` address
- `ulong` amount

为其他账户进行余额初始化。

仅可被初始化Miner调用。

## Data Structure

```Protobuf

// The miners public key list for each term.
message Miners {
    uint64 TermNumber = 1;
    repeated string PublicKeys = 2;
}

// To query to first round number of each term.
message TermNumberLookUp {
    map<uint64, uint64> Map = 1;// Term number -> Round number.
}

// All the candidates.
message Candidates {
    repeated string PublicKeys = 1;
}

// The ticket / voting information for one user.
message Tickets {
    repeated VotingRecord VotingRecords = 1;
    uint64 TotalTickets = 2;
}

// A voting record.
message VotingRecord {
    string From = 1;
    string To = 2;
    uint64 Count = 3;
    uint64 RoundNumber = 4;
    Hash TransactionId = 5;
    uint64 VoteAge = 6;
    repeated int32 LockDaysList = 7;// Can be renewed by adding items.
    uint64 UnlockAge = 8;
    uint64 TermNumber = 9;
}

// The snap shot of one term.
message TermSnapshot {
    uint64 EndRoundNumber = 1;
    uint64 TotalBlocks = 2;
    repeated CandidateInTerm CandidatesSnapshot = 3;
    uint64 TermNumber = 4;
}

// The information of a candidate of a specific term.
message CandidateInTerm {
    string PublicKey = 1;
    uint64 Votes = 2;
}

// The information of a round.
message Round {
    uint64 RoundNumber = 1;
    map<string, MinerInRound> RealTimeMinersInfo = 2;
    int32 MiningInterval = 3;
    uint64 BlockchainAge = 4;
}

// The information of a miner in a specific round.
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

// The information of a candidate.
message CandidateInHistory {
    repeated uint64 Terms = 1;
    uint64 ProducedBlocks = 2;
    uint64 MissedTimeSlots = 3;
    uint64 ContinualAppointmentCount = 4;
    uint64 ReappointmentCount = 5;
    repeated string Aliases = 6;
}

message TicketsDictionary {
    map<string, Tickets> Maps = 1;
}

message StringList {
    repeated string Values = 1;
}
```
