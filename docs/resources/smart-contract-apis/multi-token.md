# multi-token

The multi-token contract is most essentially used for managing balances.

## Token life-cycle: creation, issuance and transfer

These methods constitute the basic functionality needed to maintain balances for tokens. 

```Protobuf
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

TODO: issuer, is_burnable, lock_white_list

JSON template:

```json
{"issuer":"2KTYvsWxcnjQPNnD1zWFCm83aLvmRGAQ8bvLnLFUV7XrrnYWNv","symbol":"TOK","tokenName":"Token name","decimals":2,"isBurnable":true,"totalSupply":100000}
```

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

Issuing some amount of tokens to an address is the action of increasing that addresses balance for the given token. The total amount of issued tokens must not exceed the total supply and only the issuer (creator) of the token can issue tokens. Issuing tokens effectively increases the circulating supply (token info Supply in the contract).

The issue method takes as IssueInput message parameter:
- symbol : the symbol that identifies the token, must exist.
- amount : the amount to issue.
- to : the receiver address of the newly issued tokens.

TODO: memo

JSON template:

```json
{"to":"2KTYvsWxcnjQPNnD1zWFCm83aLvmRGAQ8bvLnLFUV7XrrnYWNv","symbol":"TOK","amount":100,"memo":"some memo"}
```

**transfer**

```Protobuf
message TransferInput {
    aelf.Address to = 1;
    string symbol = 2;
    sint64 amount = 3;
    string memo = 4;
}

rpc Transfer (TransferInput) returns (google.protobuf.Empty) { }
```

Transferring tokens simply is the action of taking a given amount of tokens from one address to another. The origin address is the signer of the transaction. The balance of the sender must be higher than the amount that's transferred.
The issue method takes as TransferInput message parameter:
- to: destination address.
- symbol: the symbol of the token to transfer.
- amount: the amount of tokens to transfer. 

Transferring 

```json
{"to":"2KTYvsWxcnjQPNnD1zWFCm83aLvmRGAQ8bvLnLFUV7XrrnYWNv","symbol":"TOK","amount":100,"memo":"some memo"}
```