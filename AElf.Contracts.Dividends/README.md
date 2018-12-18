# Dividends System

## Available Methods

### GetTermDividends

Params:
- ulong termNumber

Result type:
- ulong

To get the max dividends of provided term number.

The dividends amount of specific term depends of mined blocks of this term.

### GetTermTotalWeights

Params:
- ulong termNumber

Result type:
- ulong

To get the total weights of provided term number.

### GetAvailableDividends

Params:
- VotingRecord votingRecord
- ulong currentTermNumber

Result type:
- ulong

To get current available dividends of the provided VotingRecord instance.

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

### GetTermDividends

参数:
- ulong termNumber

返回类型:
- ulong

获取某一届为分配的最大分红额度。

某届的分红额度取决于这一届出块数量。

### GetTermTotalWeights

参数:
- ulong termNumber

返回类型:
- ulong

获取某一届的投票的总权重。

### GetAvailableDividends

参数:
- VotingRecord votingRecord
- ulong currentTermNumber

返回类型:
- ulong

用于获取所提供VotingRecord实例当前可领取分红。

## 如何计算可领取分红

分红本身的计算逻辑已经在`GetAvailableDividends`中实现。

### For a voter.

1. 使用共识合约（`Consensus Contract`）中的`GetTicketsInfo`方法获取该投票人的所有投票信息。

2. 筛选出有效的投票。

3. 使用分红合约（`Dividends Contract`）中的`GetAvailableDividends`方法获取一个`VotingRecord`实例的可领取的分红。

4. 加起来。

### For a VotingRecord

使用分红合约（`Dividends Contract`）中的`GetAvailableDividends`方法即可