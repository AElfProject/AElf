## Defining a smart contract


When writing a smart contract in AElf the first thing that need to be done is to define it so it can then be generate by our tools. AElf contracts are defined as services that are currently defined and generated with gRPC and protobuf.

As an example, here is part of the definition of our multitoken contract. Each functionality will be explained more in detail in their respective section. Note that for simplicity, the contract has been simplified to show only the essential.

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

service TokenContractImpl {
    option (aelf.csharp_state) = "AElf.Contracts.MultiToken.TokenContractState";
    option (aelf.base) = "token_contract.proto";
}

message CreateInput {
    string symbol = 1;
    sint64 totalSupply = 2;
    sint32 decimals = 3;
}

// Events
message Transferred {
    option (aelf.is_event) = true;
    Address from = 1 [(aelf.is_indexed) = true];
    Address to = 2 [(aelf.is_indexed) = true];
    string symbol = 3 [(aelf.is_indexed) = true];
    sint64 amount = 4;
}
```

For message and service definitions we the **proto3** version of the protobuf language. You probably won't need to use most of the features that are provided, but here's the [full reference](https://developers.google.com/protocol-buffers/docs/proto3) for the language.

### Service

For the service we have two different types of methods:
* Actions - these are normal smart contract methods that take input and output and usually modify the state of the chain.
* Views - these methods are special in the sense that they do not modify the state of the chain. They are usually used in some way to query the value of the contracts state.

```json
rpc Create (CreateInput) returns (google.protobuf.Empty) { }
```

The services takes protobuf messages as input and also returns protobuf messages. Note that here it returns a special message - google.protobuf.Empty - that signifies returning nothing. As a convention we append Input to any protobuf type that is destined to be a parameter to a service.

#### View option

```json
rpc GetBalance (GetBalanceInput) returns (GetBalanceOutput) {
    option (aelf.is_view) = true;
}
```

This service is annotated with a view option. This signifies that this is a readonly method and will not modify the state.

#### State option

    option (aelf.csharp_state) = "AElf.Contracts.MultiToken.TokenContractState";

#### Base option

    option (aelf.base) = "token_contract.proto";

### Messages

Here we define the concept of message as defined by the protobuf language. We heavily use these messages for calling the smart contracts and serializing their state. The following is the definition of a simple message:

```json
message CreateInput {
    string symbol = 1;
    sint64 totalSupply = 2;
    sint32 decimals = 3;
}
```

Here we see a message with three field of type string, sint64 and sint32. In the message you can use any type supported by protobuf, including composite messages where one of your messages contains another message. 

#### Event option

```json
message Transferred {
    option (aelf.is_event) = true;
    Address from = 1 [(aelf.is_indexed) = true];
    Address to = 2 [(aelf.is_indexed) = true];
    string symbol = 3 [(aelf.is_indexed) = true];
    sint64 amount = 4;
}
```

#### Indexed option

```json
message Transferred {
    option (aelf.is_event) = true;
    Address from = 1 [(aelf.is_indexed) = true];
    Address to = 2 [(aelf.is_indexed) = true];
    string symbol = 3 [(aelf.is_indexed) = true];
    sint64 amount = 4;
}
```

