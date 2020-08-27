# Economic Contract

The Economic contract establishes the economic system of the AElf. When the block chain starts to work, this contract will initialize other contracts related to economic activities.

## **Actions**

### **InitialEconomicSystem**

```Protobuf
rpc InitialEconomicSystem (InitialEconomicSystemInput) returns (google.protobuf.Empty){}

message InitialEconomicSystemInput {
    string native_token_symbol = 1;
    string native_token_name = 2;
    int64 native_token_total_supply = 3;
    int32 native_token_decimals = 4;
    bool is_native_token_burnable = 5;
    int64 mining_reward_total_amount = 6;
    int64 transaction_size_fee_unit_price = 7;
}
```

It will initialize other contracts related to economic activities (For instance, create the native token). This transaction only can be send once because after the first sending, its state will be set to initialized.

- **IssueNativeTokenInput**
  - **native token symbol**: the native token symbol.
  - **native token name**: the native token name.
  - **native token total supply**: the native token total supply.
  - **native token decimals**: the token calculation is accurated to which decimal.
  - **is native token burnable**: it indicaites if the token is burnable.
  - **mining reward total amount**: It determines how much native token is used to reward the miners.
  - **transaction size fee unit price**: the transaction fee = transaction size * unit fee.

### **IssueNativeToken**

```Protobuf
rpc IssueNativeToken (IssueNativeTokenInput) returns (google.protobuf.Empty) {}

message IssueNativeTokenInput {
    int64 amount = 1;
    string memo = 2;
    aelf.Address to = 3;
}
```

Only ZeroContract is able to issue the native token.

- **IssueNativeTokenInput**
  - **amount**: amount of token.
  - **memo**: memo.
  - **to**: the recipient of the token.

## **Acs1 specific methods**

For reference, you can find here the methods implementing acs1.

### SetMethodFee

```Protobuf
rpc SetMethodFee (MethodFees) returns (google.protobuf.Empty){}

message MethodFees {
    string method_name = 1;
    repeated MethodFee fees = 2;
}

message MethodFee {
    string symbol = 1;
    int64 basic_fee = 2;
}
```

It sets method fee.

- **MethodFees**
  - **method name**: the method name in this contract.
  - **fees**: fee list.

- **MethodFee**
  - **symbol**: token symbol.
  - **basic fee**: fee.

### ChangeMethodFeeController

```Protobuf
rpc ChangeMethodFeeController (AuthorityInfo) returns (google.protobuf.Empty){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

Change the method fee controller, the default is Parliament.

- **AuthorityInfo**
  - **contract address**: new controller's contract address.
  - **owner address**: new controller's address.

### GetMethodFee

```Protobuf
rpc GetMethodFee (google.protobuf.StringValue) returns (MethodFees){}

message MethodFees {
    string method_name = 1;
    repeated MethodFee fees = 2;
}
```

This mehtod is used to query the method fee information.

note: *for MethodFees see SetMethodFee*

### GetMethodFeeController

```Protobuf
rpc GetMethodFeeController (google.protobuf.Empty) returns (acs1.AuthorityInfo){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

This mehtod is used to query the controller of the method fee.

note: *for AuthorityInfo see ChangeMethodFeeController*
