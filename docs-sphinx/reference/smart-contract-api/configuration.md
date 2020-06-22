# Configuration Contract

This contract's controller(the default is parliament) is able to save data(configuration) on the block chain.

## **Actions**

### **SetConfiguration**

```Protobuf
rpc SetConfiguration (SetConfigurationInput) returns (google.protobuf.Empty){}

message SetConfigurationInput {
    string key = 1;
    bytes value = 2;
}
```

This method is used to add or reset configurations.

- **SetConfigurationInput**
  - **key**: the configuration's key.
  - **value**: the configuration's value(bianry data).

### **ChangeConfigurationController**

```Protobuf
rpc ChangeConfigurationController (acs1.AuthorityInfo) returns (google.protobuf.Empty){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

The controller can be transfer to others.

- **AuthorityInfo**
  - **contract address**: new controller's contract address.
  - **owner address**: new controller's address.

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
  - **omethod name**: the method name in this contract.
  - **fees**: fee list.

- **MethodFee**:
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

note: *for AuthorityInfo see ChangeConfigurationController*

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

This mehtod is used to query the method fee information.

note: *for AuthorityInfo see ChangeMethodFeeController*

## **View methods**

For reference, you can find here the available view methods.

### GetConfiguration

```Protobuf
rpc GetConfiguration (google.protobuf.StringValue) returns (google.protobuf.BytesValue){}

message StringValue {
  string value = 1;
}

message BytesValue {
  bytes value = 1;
}
```

This method is used to query a configurations.

- **StringValue**
  - **value**: the configuration's key.

- **Returns**
  - **value**: the configuration's data(bianry).

### GetConfigurationController

This method is used to query the controller information.

```Protobuf
rpc GetConfigurationController (google.protobuf.Empty) returns (acs1.AuthorityInfo){}

message AuthorityInfo {
    aelf.Address contract_address = 1;
    aelf.Address owner_address = 2;
}
```

- **Returns**
  - **contract address**: new controller's contract address.
  - **owner address**: new controller's address.
