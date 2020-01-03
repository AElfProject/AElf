# Treasury Contract

The Treasury contract is essentially used for distributing bonus to voters and candidates in the election.

## **Donate**

Donate token from caller to treasury virtual address. If the token is not native token in the current chain, it will be transferred to the native token firstly.

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

After a successful donate, a **DonationReceived** event log can be found in the transaction result.

**DonationReceived**:
- **from**: from address.
- **to**: to address.
- **symbol**: token symbol.
- **amount**: amount of token.
- **memo**: memo.

## **Donate all tokens**

Donate all token(transfer to native token) from caller to treasury virtual address. (by call Donate described above).

```Protobuf
rpc DonateAll (DonateAllInput) returns (google.protobuf.Empty) {}

message DonateAllInput {
    string symbol = 1;
}
```

**DonateAllInput**:
- **symbol**: token symbol.

## view methods

For reference, you can find here the available view methods.

### GetCurrentTreasuryBalance

Get total balance of the native token on the Treasury virtual address.

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
