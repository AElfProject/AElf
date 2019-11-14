# multi-token

The multi-token contract is most essentially used for managing balances.

## Token life-cycle: creation, issuance and transfer

These methods constitute the basic functionality needed to maintain balances for tokens. 

``` Protobuf
rpc Create (CreateInput) returns (google.protobuf.Empty) { }
rpc Issue (IssueInput) returns (google.protobuf.Empty) { }
rpc Transfer (TransferInput) returns (google.protobuf.Empty) { }
rpc TransferFrom (TransferFromInput) returns (google.protobuf.Empty) { }
```

**Creation**:
``` Protobuf

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

rpc Create (CreateInput) returns (google.protobuf.Empty) { }
```

The token contract permits the creation of an entirely new token and the first action needed before using a token is its creation. You will need to choose the following:

-  a **symbol**: a short string between 1 and 8 characters composed only of upper-case letters like for example "ELF" or "AETC" (no numbers allowed). Of course, since tokens are uniquely identified by the symbol, it must not already exist.
-  a **token name**: a more descriptive name for your token or the long name. For example, "RMB" could be the token symbol and "RenMinBi" the token's name. This is non-optional field up to 80 characters in length. 
- the **total supply** for the token, that is the amount of tokens that will exist. This must be larger than 0.
- **decimals**: a positive integer between 0-18.
- **issue_chain_id**: the if of the chain, this defaults to the chain id of the node.

TODO: issuer, is_burnable, lock_white_list, is_transfer_disabled

The creation method on the token contract takes an **CreateInput** message as define bellow:

**issuance**

``` Protobuf
message IssueInput {
    string symbol = 1;
    sint64 amount = 2;
    string memo = 3;
    aelf.Address to = 4;
}

rpc Issue (IssueInput) returns (google.protobuf.Empty) { }
```

**transfer**

``` Protobuf
message TransferInput {
    aelf.Address to = 1;
    string symbol = 2;
    sint64 amount = 3;
    string memo = 4;
}

rpc Transfer (TransferInput) returns (google.protobuf.Empty) { }
```