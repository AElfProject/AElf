# Configuration Contract

This contract's controller(the default is parliament) is able to save data(configuration) on the block chain.

## **SetConfiguration**

This method is used to add or reset configurations.

```Protobuf
rpc SetConfiguration (SetConfigurationInput) returns (google.protobuf.Empty) {
}

message SetConfigurationInput {
    string key = 1;
    bytes value = 2;
}
```

**SetConfigurationInput**:

- **key**: the configuration's key.
- **value**: the configuration's value(bianry data).

## **ChangeConfigurationController**

The controller can be transfer to others.

```Protobuf
rpc ChangeConfigurationController (acs1.AuthorityInfo) returns (google.protobuf.Empty) {
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

**AuthorityInfo**:

- **contract address**: new controller's contract address.
- **owner address**: new controller's address.

## ACS1 Implementation

For reference, you can find here the methods implementing acs1.

### SetMethodFee

It sets method fee.

```Protobuf
rpc SetMethodFee (MethodFees) returns (google.protobuf.Empty){
}

message MethodFees {
    string method_name = 1;
    repeated MethodFee fees = 2;
}

message MethodFee {
    string symbol = 1;
    int64 basic_fee = 2;
}
```

**MethodFees**:

- **method name** the method name in this contract.
- **fees** fee list.

**MethodFee**:

- **symbol** token symbol.
- **basic fee** fee.

### ChangeMethodFeeController

Change the method fee controller, the default is Parliament.

```Protobuf
rpc ChangeMethodFeeController (AuthorityInfo) returns (google.protobuf.Empty) {
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

**AuthorityInfo**:

- **contract address**: new controller's contract address.
- **owner address**: new controller's address.

### GetMethodFee

This mehtod is used to query the method fee information.

```Protobuf
rpc GetMethodFee (google.protobuf.StringValue) returns (MethodFees) {
}

message MethodFees {
    string method_name = 1;
    repeated MethodFee fees = 2;
}
```

note: *for MethodFees see SetMethodFee*

### GetMethodFeeController

This mehtod is used to query the method fee information.

```Protobuf
rpc GetMethodFeeController (google.protobuf.Empty) returns (acs1.AuthorityInfo) { 
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

note: *for AuthorityInfo see ChangeMethodFeeController*

## view methods

For reference, you can find here the available view methods.

### GetConfiguration

This method is used to query a configurations.

```Protobuf
rpc GetConfiguration (google.protobuf.StringValue) returns (google.protobuf.BytesValue) {
}

message StringValue {
  string value = 1;
}

message BytesValue {
  bytes value = 1;
}
```

**StringValue**:

- **value**: the configuration's key.

**returns**:

- **value** the configuration's data(bianry).

### GetConfigurationController

This method is used to query the controller information.

```Protobuf
rpc GetConfigurationController (google.protobuf.Empty) returns (acs1.AuthorityInfo) {
}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

**returns**:

- **contract address**: new controller's contract address.
- **owner address**: new controller's address.
