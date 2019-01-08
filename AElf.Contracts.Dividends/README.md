# Dividends System

## Available Methods

### `GetTermDividends`

Params:
- `ulong` termNumber

Result type:
- `ulong`

To get the max dividends of provided term number.

The dividends amount of specific term depends of mined blocks of this term.

### `GetTermTotalWeights`

Params:
- `ulong` termNumber

Result type:
- `ulong`

To get the total weights of provided term number.

### `GetAvailableDividends`

Params:
- `VotingRecord` votingRecord

Result type:
- `ulong`

To get current available dividends of the provided VotingRecord instance.

### `GetAllAvailableDividends`

Params:
- `string` publicKyeToHex

Result type:
- `ulong`

To get current available dividends of the provided public key.

### `GetAvailableDividendsByVotingInformation`

Params:
- `Hash` transctionId
- `ulong` termNumber
- `ulong` weight

Result type:
- `ulong`

To get current available dividends of the provided voting information.

### `CheckDividends`

Params:
- `ulong` ticketsAmount
- `int` lockTime
- `ulong` termNumber

Result type:
- `ulong`

To check the final dividends of a given voting of provided term.

### `CheckDividendsOfPreviousTerm` / `CheckDividendsOfPreviousTermToFriendlyString`

*No params*

Result Type:
- `ULongList` / `string`

10000 tickets of previous term, locking 30, 180, 365, 730, 1095.

## How to calculate dividends

### For a voter.

1. `GetTicketsInfo` to `Consensus Contract` to get all the voting information of this voter.

2. Choose useful `VotingRecord`s.

3. `GetAvailableDividends` to `Dividends Contract` to get the available dividends of one `VotingRecord`. 

4. Sum up.

### For a VotingRecord

`GetAvailableDividends` to `Dividends Contract`

# 分红系统

## 可用方法

### `GetTermDividends`

参数:
- `ulong` termNumber

返回类型:
- `ulong`

获取某一届为分配的最大出块奖励分红额度。

某届的出块奖励分红额度取决于这一届出块数量。

### `GetTermTotalWeights`

参数:
- `ulong` termNumber

返回类型:
- `ulong`

获取某一届的投票的总权重。

### `GetAvailableDividends`

参数:
- `VotingRecord` votingRecord

返回类型:
- `ulong`

用于获取所提供VotingRecord实例当前可领取的出块奖励分红。

### `GetAllAvailableDividends`

参数:
- `string` publicKyeToHex

返回类型:
- `ulong`

用于获取所提供私钥当前可领取的出块奖励分红。

### `GetAvailableDividendsByVotingInformation`

参数：
- `Hash` transctionId
- `ulong` termNumber
- `ulong` weight

返回类型:
- `ulong`

用于获取所提供投票信息相关投票实例当前可领取的出块奖励分红。

### `CheckDividends`

参数:
- `ulong` ticketsAmount
- `int` lockTime
- `ulong` termNumber

返回类型:
- `ulong`

查看某投票参数在某一届可领取的出块奖励分红。

### `CheckDividendsOfPreviousTerm` / `CheckDividendsOfPreviousTermToFriendlyString`

*无参数*

返回类型:
- `ULongList` / `string`

查看10000票在上一届分别锁仓30、180、365、730、1095天可获取分红。

## 如何计算可领取分红

分红本身的计算逻辑已经在`GetAvailableDividends`中实现。

### For a voter.

1. 使用共识合约（`Consensus Contract`）中的`GetTicketsInfo`方法获取该投票人的所有投票信息。

2. 筛选出有效的投票。

3. 使用分红合约（`Dividends Contract`）中的`GetAvailableDividends`方法获取一个`VotingRecord`实例的可领取的分红。

4. 加起来。

### For a VotingRecord

使用分红合约（`Dividends Contract`）中的`GetAvailableDividends`方法即可