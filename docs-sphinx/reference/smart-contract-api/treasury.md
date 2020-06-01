# Treasury Contract

The Treasury contract is essentially used for distributing bonus' to voters and candidates during the election process.

## **Donate**

Donates tokens from the caller to the treasury. If the tokens are not native tokens in the current chain, they will be first converted to the native token.

```Protobuf
rpc Donate (DonateInput) returns (google.protobuf.Empty) {}

message DonateInput {
    string symbol = 1;
    sint64 amount = 2;
}

message DonationReceived {
    aelf.Address from = 1 [(aelf.is_indexed) = true];
    aelf.Address to = 2 [(aelf.is_indexed) = true];
    string symbol = 3 [(aelf.is_indexed) = true];
    sint64 amount = 4 [(aelf.is_indexed) = true];
    string memo = 5;
}
```

**DonateInput**:
- **symbol**: token symbol.
- **amount**: token amount.

After a successful donation a **DonationReceived** event log can be found in the transaction result.

**DonationReceived**:
- **from**: from address.
- **to**: to address.
- **symbol**: token symbol.
- **amount**: amount of token.
- **memo**: memo.

## **Donate all tokens**

Donate all token (transfer to native token) from caller to the treasury (by calling **Donate** described above).

```Protobuf
rpc DonateAll (DonateAllInput) returns (google.protobuf.Empty) {}

message DonateAllInput {
    string symbol = 1;
}
```

**DonateAllInput**:
- **symbol**: token symbol.

## **SetDistributingSymbolList**

Set a token list that can be used to distribute.

```Protobuf
rpc SetDistributingSymbolList (SymbolList) returns (google.protobuf.Empty) {}

message SymbolList {
    repeated string value = 1;
}
```

**SymbolList**:
- **value**: token symbol list.

## **SetDividendPoolWeightSetting**

Set weight for the three activities.

```Protobuf
rpc SetDividendPoolWeightSetting (DividendPoolWeightSetting) returns (google.protobuf.Empty){}

message DividendPoolWeightSetting {
    int32 citizen_welfare_weight = 1;
    int32 backup_subsidy_weight = 2;
    int32 miner_reward_weight = 3;
}
```

**DividendPoolWeightSetting**:
- **citizen welfare weight**: citizen welfare weight.
- **backup subsidy weight**: backup subsidy weight.
- **miner reward weight**: miner reward weight.

## **SetMinerRewardWeightSetting**

Set weight for the three activities composing of miner reward activity.

```Protobuf
rpc SetMinerRewardWeightSetting (MinerRewardWeightSetting) returns (google.protobuf.Empty){}

message MinerRewardWeightSetting {
    int32 basic_miner_reward_weight = 1;
    int32 votes_weight_reward_weight = 2;
    int32 re_election_reward_weight = 3;
}
```

**MinerRewardWeightSetting**:
- **basic miner reward weight**: basic miner reward weight.
- **votes weight reward weight**: votes weight reward weight.
- **re-election reward weight**: re-election reward weight.

## **ChangeTreasuryController**

Change the controller who is able to update symbol list and activities' weight above.

```Protobuf
rpc ChangeTreasuryController (acs1.AuthorityInfo) returns (google.protobuf.Empty) {}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

**AuthorityInfo**:
- **contract address**: controller type.
- **owner address**: controller's address.

## view methods

For reference, you can find here the available view methods.

### GetCurrentTreasuryBalance

Get the Treasury's total balance of the native token from the Treasury.

```Protobuf
rpc GetCurrentTreasuryBalance (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

**returns**:
- **value**: amount of native token.

### GetWelfareRewardAmountSample

Test the welfare bonus gotten base on 10000 Vote Token. The input is a array of locking time, and the output is the corresponding welfare. 

```Protobuf
rpc GetWelfareRewardAmountSample (GetWelfareRewardAmountSampleInput) returns (GetWelfareRewardAmountSampleOutput) {}

message GetWelfareRewardAmountSampleInput {
    repeated sint64 value = 1;
}

message GetWelfareRewardAmountSampleOutput {
    repeated sint64 value = 1;
}
```

**GetWelfareRewardAmountSampleInput**:
- **value**: a array of locking time.

**returns**:
- **value**: a array of welfare.

### GetTreasurySchemeId

Get treasury scheme id. If it does not exist, it will return hash.empty.

```Protobuf
rpc GetTreasurySchemeId (google.protobuf.Empty) returns (aelf.Hash) {}

message Hash
{
    bytes value = 1;
}
```

**returns**:
- **value**: scheme id.

### GetDistributingSymbolList

Get the symbol list that can be used to distribute.

```Protobuf
rpc GetDistributingSymbolList (google.protobuf.Empty) returns (SymbolList) {}

message SymbolList {
    repeated string value = 1;
}
```

note: *for SymbolList see SetDistributingSymbolList*

### GetDividendPoolWeightProportion

Get activities's weight expressed as a percentage

```Protobuf
rpc GetDividendPoolWeightProportion (google.protobuf.Empty) returns (DividendPoolWeightProportion){}

message DividendPoolWeightProportion {
    SchemeProportionInfo citizen_welfare_proportion_info = 1;
    SchemeProportionInfo backup_subsidy_proportion_info = 2;
    SchemeProportionInfo miner_reward_proportion_info = 3;
}

message SchemeProportionInfo{
    aelf.Hash scheme_id = 1;
    int32 proportion = 2;
}
```

**returns**:
- **citizen welfare proportion info**: citizen welfare proportion info.
- **backup subsidy proportion info**: backup subsidy proportion info.
- **miner reward proportion info**: miner reward proportion info.

**SchemeProportionInfo**:
- **scheme id**: scheme id
- **proportion**: the weight expressed as a percentage.

### GetMinerRewardWeightProportion

Get the weight expressed as a percentage of the activities composing of miner reward.

```Protobuf
rpc GetMinerRewardWeightProportion (google.protobuf.Empty) returns (MinerRewardWeightProportion){}

message MinerRewardWeightProportion {
    SchemeProportionInfo basic_miner_reward_proportion_info = 1;
    SchemeProportionInfo votes_weight_reward_proportion_info = 2;
    SchemeProportionInfo re_election_reward_proportion_info = 3;
}
```

note: *for MinerRewardWeightProportion see GetDividendPoolWeightProportion*

### GetTreasuryController

Get this contract's controller.

```Protobuf
rpc GetTreasuryController (google.protobuf.Empty) returns (acs1.AuthorityInfo) {}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

note: *for AuthorityInfo see ChangeTreasuryController*
