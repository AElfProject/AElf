# multi-token

The multi-token contract is most essentially used for managing balances.

**State:**
MappedState string -> Token-Info


## Token life-cycle: creation, issuance and transfer

These methods constitute the basic functionality needed to maintain balances for tokens. 

``` Protobuf
rpc Create (CreateInput) returns (google.protobuf.Empty) { }
rpc Issue (IssueInput) returns (google.protobuf.Empty) { }
rpc Transfer (TransferInput) returns (google.protobuf.Empty) { }
rpc TransferFrom (TransferFromInput) returns (google.protobuf.Empty) { } 

message CreateInput {
    string symbol = 1;
    string tokenName = 2;
    sint64 totalSupply = 3;
    sint32 decimals = 4;
    aelf.Address issuer = 5;
    bool is_burnable = 6;
    repeated aelf.Address lock_white_list = 7;
    bool is_transfer_disabled = 8;
    int32 issue_chain_id = 9;
}

message Issue/TransferInput {
    string symbol = 1;
    sint64 amount = 2;
    string memo = 3;
    aelf.Address to = 4;
}
```

**Creation**:
``` Protobuf
rpc Create (CreateInput) returns (google.protobuf.Empty) { }
```

The token contract permits the creation of an entirely new token. For this a **symbol** and a **token** must be provided. 
The token symbol must be an upper-case

**issuance**

``` Protobuf
rpc Issue (IssueInput) returns (google.protobuf.Empty) { }
```

**transfer**

``` Protobuf
rpc Transfer (TransferInput) returns (google.protobuf.Empty) { }
rpc TransferFrom (TransferFromInput) returns (google.protobuf.Empty) { } 
```