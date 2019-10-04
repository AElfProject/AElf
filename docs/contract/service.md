## Smart contract service

When writing a smart contract in AElf the first thing that need to be done is to define it so it can then be generate by our tools. AElf contracts are defined as services that are currently defined and generated with gRPC and protobuf.

As an example, here is part of the definition of our multitoken contract. Each functionality will be explained more in detail in their respective sections. Note that for simplicity, the contract has been simplified to show only the essential.

```json
syntax = "proto3";

package token;
import "common.proto";
option csharp_namespace = "AElf.Contracts.MultiToken.Messages";

service TokenContract {
    option (aelf.csharp_state) = "AElf.Contracts.MultiToken.TokenContractState";

    // Actions
    rpc Create (CreateInput) returns (google.protobuf.Empty) { }
    rpc Transfer (TransferInput) returns (google.protobuf.Empty) { }

    // Views
    rpc GetBalance (GetBalanceInput) returns (GetBalanceOutput) {
        option (aelf.is_view) = true;
    }
}

```

For the service we have two different types of methods:
* Actions - these are normal smart contract methods that take input and output and usually modify the state of the chain.
* Views - these methods are special in the sense that they do not modify the state of the chain. They are usually used in some way to query the value of the contracts state.

```json
rpc Create (CreateInput) returns (google.protobuf.Empty) { }
```

The services takes a protobuf message as input and also returns a protobuf message as output. Note that here it returns a special message - google.protobuf.Empty - that signifies returning nothing. As a convention we append Input to any protobuf type that is destined to be a parameter to a service.


#### View option

```json
rpc GetBalance (GetBalanceInput) returns (GetBalanceOutput) {
    option (aelf.is_view) = true;
}
```

This service is annotated with a view option. This signifies that this is a readonly method and will not modify the state.