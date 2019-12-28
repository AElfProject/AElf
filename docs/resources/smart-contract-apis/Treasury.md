# Treasury Contract
The Treasury contract is essentially used for distributing bonus to voters and candidates in the election.

## ** description**:

Donate token from caller to treasury virtual address. If the token is not native token in the current chain, it will be transferred to the native token firstly.
```Protobuf
rpc Donate (DonateInput) returns (google.protobuf.Empty) {}

message DonateInput {
    string symbol = 1;
    sint64 amount = 2;
}
```
DonateInput
- **symbol**  token symbol
- **amount**  token amount





Donate all token(transfer to native token) from caller to treasury virtual address.
```Protobuf
rpc DonateAll (DonateAllInput) returns (google.protobuf.Empty) {}

message DonateAllInput {
    string symbol = 1;
}
```

DonateAllInput
- **symbol** token symbol







## view methods

For reference, you can find here the available view methods.


Get total balance of the native token on the Treasury virtual address
```Protobuf
rpc GetCurrentTreasuryBalance (google.protobuf.Empty)returns(aelf.SInt64Value){}

message SInt64Value
{
    sint64 value = 1;
}
```

SInt64Value
- **value** amount of native token



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

GetWelfareRewardAmountSampleInput
- **value** a array of locking time

GetWelfareRewardAmountSampleOutput
- **value** a array of welfare



Get treasury scheme id. If it does not exist, it will return hash.empty.
```Protobuf
rpc GetTreasurySchemeId (google.protobuf.Empty) returns (aelf.Hash) {}

message Hash
{
    bytes value = 1;
}
```
Hash
- **value**  scheme id
