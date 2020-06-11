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
