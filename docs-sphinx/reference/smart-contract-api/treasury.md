# Treasury Contract

The Treasury contract is essentially used for distributing bonus' to voters and candidates during the election process.

## **Actions**

### **Donate**

```Protobuf
rpc Donate (DonateInput) returns (google.protobuf.Empty){}

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

Donates tokens from the caller to the treasury. If the tokens are not native tokens in the current chain, they will be first converted to the native token.

- **DonateInput**
  - **symbol**: token symbol.
  - **amount**: token amount.

- **Event**
  - **DonationReceived**
    - **from**: from address.
    - **to**: to address.
    - **symbol**: token symbol.
    - **amount**: amount of token.
    - **memo**: memo.

### **Donate all tokens**

```Protobuf
rpc DonateAll (DonateAllInput) returns (google.protobuf.Empty){}

message DonateAllInput {
    string symbol = 1;
}
```

Donate all token (transfer to native token) from caller to the treasury (by calling **Donate** described above).

- **DonateAllInput**
  - **symbol**: token symbol.

### **SetDistributingSymbolList**

```Protobuf
rpc SetDistributingSymbolList (SymbolList) returns (google.protobuf.Empty){}

message SymbolList {
    repeated string value = 1;
}
```

Set a token list that can be used to distribute.

- **SymbolList**
  - **value**: token symbol list.

### **SetDividendPoolWeightSetting**

```Protobuf
rpc SetDividendPoolWeightSetting (DividendPoolWeightSetting) returns (google.protobuf.Empty){}

message DividendPoolWeightSetting {
    int32 citizen_welfare_weight = 1;
    int32 backup_subsidy_weight = 2;
    int32 miner_reward_weight = 3;
}
```

Set weight for the three activities.

- **DividendPoolWeightSetting**
  - **citizen welfare weight**: citizen welfare weight.
  - **backup subsidy weight**: backup subsidy weight.
  - **miner reward weight**: miner reward weight.

### **SetMinerRewardWeightSetting**

```Protobuf
rpc SetMinerRewardWeightSetting (MinerRewardWeightSetting) returns (google.protobuf.Empty){}

message MinerRewardWeightSetting {
    int32 basic_miner_reward_weight = 1;
    int32 votes_weight_reward_weight = 2;
    int32 re_election_reward_weight = 3;
}
```

Set weight for the three activities composing of miner reward activity.

- **MinerRewardWeightSetting**
  - **basic miner reward weight**: basic miner reward weight.
  - **votes weight reward weight**: votes weight reward weight.
  - **re-election reward weight**: re-election reward weight.

### **ChangeTreasuryController**

```Protobuf
rpc ChangeTreasuryController (acs1.AuthorityInfo) returns (google.protobuf.Empty) {}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Change the controller who is able to update symbol list and activities' weight above.

- **AuthorityInfo**
  - **contract address**: controller type.
  - **owner address**: controller's address.

## **View methods**

For reference, you can find here the available view methods.

### GetCurrentTreasuryBalance

```Protobuf
rpc GetCurrentTreasuryBalance (google.protobuf.Empty) returns (aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

Get the Treasury's total balance of the native token from the Treasury.

- **Returns**
  - **value**: amount of native token.

### GetWelfareRewardAmountSample

```Protobuf
rpc GetWelfareRewardAmountSample (GetWelfareRewardAmountSampleInput) returns (GetWelfareRewardAmountSampleOutput){}

message GetWelfareRewardAmountSampleInput {
    repeated sint64 value = 1;
}

message GetWelfareRewardAmountSampleOutput {
    repeated sint64 value = 1;
}
```

Test the welfare bonus gotten base on 10000 Vote Token. The input is a array of locking time, and the output is the corresponding welfare.

- **GetWelfareRewardAmountSampleInput**
  - **value**: a array of locking time.

- **Returns**
  - **value**: a array of welfare.

### GetTreasurySchemeId

```Protobuf
rpc GetTreasurySchemeId (google.protobuf.Empty) returns (aelf.Hash) {}

message Hash
{
    bytes value = 1;
}
```

Get treasury scheme id. If it does not exist, it will return hash.empty.

- **Returns**
  - **value**: scheme id.

### GetDistributingSymbolList

```Protobuf
rpc GetDistributingSymbolList (google.protobuf.Empty) returns (SymbolList){}

message SymbolList {
    repeated string value = 1;
}
```

Get the symbol list that can be used to distribute.

note: *for SymbolList see SetDistributingSymbolList*

### GetDividendPoolWeightProportion

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

Get activities's weight expressed as a percentage

- **Returns**
  - **citizen welfare proportion info**: citizen welfare proportion info.
  - **backup subsidy proportion info**: backup subsidy proportion info.
  - **miner reward proportion info**: miner reward proportion info.

- **SchemeProportionInfo**
  - **scheme id**: scheme id
  - **proportion**: the weight expressed as a percentage.

### GetMinerRewardWeightProportion

```Protobuf
rpc GetMinerRewardWeightProportion (google.protobuf.Empty) returns (MinerRewardWeightProportion){}

message MinerRewardWeightProportion {
    SchemeProportionInfo basic_miner_reward_proportion_info = 1;
    SchemeProportionInfo votes_weight_reward_proportion_info = 2;
    SchemeProportionInfo re_election_reward_proportion_info = 3;
}
```

Get the weight expressed as a percentage of the activities composing of miner reward.

note: *for MinerRewardWeightProportion see GetDividendPoolWeightProportion*

### GetTreasuryController

```Protobuf
rpc GetTreasuryController (google.protobuf.Empty) returns (acs1.AuthorityInfo){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Get this contract's controller.

note: *for AuthorityInfo see ChangeTreasuryController*
