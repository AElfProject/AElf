# multi-token

The multi-token contract is most essentially used for managing balances.

## Token life-cycle: creation, issuance and transfer.

These methods constitute the basic functionality needed to maintain balances for tokens. For a full listing of the contracts methods you can check the [Token Contract definition](https://github.com/AElfProject/AElf/blob/master/protobuf/token_contract.proto) on GitHub.

## **Create**

```Protobuf
rpc Create (CreateInput) returns (google.protobuf.Empty) { }

message CreateInput {
    string symbol = 1;
    string token_name = 2;
    sint64 total_supply = 3;
    sint32 decimals = 4;
    aelf.Address issuer = 5;
    bool is_burnable = 6;
    repeated aelf.Address lock_white_list = 7;
    bool is_profitable = 8;
    int32 issue_chain_id = 9;
}
```

The token contract permits the creation of an entirely new token and the first action needed before using a token is its creation. The **Create** method takes exactly on parameter, a **CreateInput** message.

- **issuer** is the creator of this token.
- **symbol** is a short string between 1 and 8 characters composed only of upper-case letters like for example "ELF" or "AETC" (no numbers allowed). Of course, since tokens are uniquely identified by the symbol, it must not already exist.
- **token_name** is a more descriptive name for your token or the long name. For example, "RMB" could be the token symbol and "RenMinBi" the token's name. This is a non-optional field up to 80 characters in length. 
- **total_supply** for the token is the amount of tokens that will exist. This must be larger than 0.
- **decimals** is a positive integer between 0-18.
- **issue_chain_id** is the id of the chain, this defaults to the chain id of the node.

## **Issue**

```Protobuf
rpc Issue (IssueInput) returns (google.protobuf.Empty) { }

message IssueInput {
    string symbol = 1;
    sint64 amount = 2;
    string memo = 3;
    aelf.Address to = 4;
}
```

Issuing some amount of tokens to an address is the action of increasing that addresses balance for the given token. The total amount of issued tokens must not exceed the total supply of the token and only the issuer (creator) of the token can issue tokens. Issuing tokens effectively increases the circulating supply. The **Issue** method takes exactly one parameter, a **IssueInput** message.

- **symbol** is the symbol that identifies the token, it must exist.
- **amount** is the amount to issue.
- **to** field the receiver address of the newly issued tokens.
- **memo** optionally you can specify a later accessible when parsing the transaction. 

## **Transfer**

```Protobuf
rpc Transfer (TransferInput) returns (google.protobuf.Empty) { }

message TransferInput {
    aelf.Address to = 1;
    string symbol = 2;
    sint64 amount = 3;
    string memo = 4;
}
```

Transferring tokens simply is the action of transferring a given amount of tokens from one address to another. The origin or source address is the signer of the transaction. The balance of the sender must be higher than the amount that is transferred.
The **Transfer** method takes exactly one parameter, a **TransferInput** message.

- **to** field is the receiver of the tokens.
- **symbol** is the symbol that identifies the token, it must exist.
- **amount** is the amount to to transfer.
- **memo** optionally you can specify a later accessible when parsing the transaction. 
