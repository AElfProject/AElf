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

## **TransferFrom**

```Protobuf
rpc TransferFrom (TransferFromInput) returns (google.protobuf.Empty) { }

message TransferFromInput {
    aelf.Address from = 1;
    aelf.Address to = 2;
    string symbol = 3;
    sint64 amount = 4;
    string memo = 5;
}
```

- **from**
- **to**
- **symbol**
- **amount**
- **memo**

## Allowances.

Allowances allow some entity (in fact an address in this case) to authorize another address to transfer tokens on his behalf. There are two methods available for controlling this, namely **Approve** and **UnApprove**, that take as input respectively, a ApproveInput and UnApproveInput message (both define the same fields).

## **Approve**

``` Proto
rpc Approve (ApproveInput) returns (google.protobuf.Empty) { }

message ApproveInput {
    aelf.Address spender = 1;
    string symbol = 2;
    sint64 amount = 3;
}
```

- **spender**
- **symbol**
- **amount**

## **UnApprove**

``` Proto
rpc UnApprove (UnApproveInput) returns (google.protobuf.Empty) { }

message UnApproveInput {
    aelf.Address spender = 1;
    string symbol = 2;
    sint64 amount = 3;
}
```

- **spender**
- **symbol**
- **amount**

## Locking.

## **Lock**

``` Proto
rpc Lock (LockInput) returns (google.protobuf.Empty) { }

message LockInput {
    aelf.Address address = 1; // The one want to lock his token.
    aelf.Hash lock_id = 2;
    string symbol = 3;
    string usage = 4;
    int64 amount = 5;
}
```

- **address** 
- **lock_id**
- **symbol**
- **usage**
- **amount**

## **Unlock**

``` Proto
rpc Unlock (UnlockInput) returns (google.protobuf.Empty) { }

message UnlockInput {
    aelf.Address address = 1; // The one want to lock his token.
    aelf.Hash lock_id = 2;
    string symbol = 3;
    string usage = 4;
    int64 amount = 5;
}
```

- **address** 
- **lock_id**
- **symbol**
- **usage**
- **amount**

## Burning tokens.

## **Burn**

``` Proto
rpc Burn (BurnInput) returns (google.protobuf.Empty) { }

message BurnInput {
    string symbol = 1;
    sint64 amount = 2;
}
```

- **symbol**
- **amount**

## View methods

## **GetTokenInfo**

``` Proto
rpc GetTokenInfo (GetTokenInfoInput) returns (TokenInfo) { }

message GetTokenInfoInput {
    string symbol = 1;
}
```

- **symbol**

## **GetNativeTokenInfo**

``` Proto
rpc GetNativeTokenInfo (google.protobuf.Empty) returns (TokenInfo) { }

message TokenInfo {
    string symbol = 1;
    string token_name = 2;
    sint64 supply = 3;
    sint64 total_supply = 4;
    sint32 decimals = 5;
    aelf.Address issuer = 6;
    bool is_burnable = 7;
    bool is_profitable = 8;
    sint32 issue_chain_id = 9;
    sint64 burned = 10;
}

```

## **GetResourceTokenInfo**

``` Proto
rpc GetResourceTokenInfo (google.protobuf.Empty) returns (TokenInfoList) { }

message TokenInfoList {
    repeated TokenInfo value = 1;
}
```

note: *for TokenInfo see GetNativeTokenInfo*

- **value** (TokenInfo) 

## **GetBalance**

``` Proto
rpc GetBalance (GetBalanceInput) returns (GetBalanceOutput) { }

message GetBalanceInput {
    string symbol = 1;
    aelf.Address owner = 2;
}

message GetBalanceOutput {
    string symbol = 1;
    aelf.Address owner = 2;
    sint64 balance = 3;
}
```

Input: 
- **symbol**
- **owner**

Output:
- **symbol**
- **owner**
- **balance**

## **GetAllowance**

``` Proto
rpc GetAllowance (GetAllowanceInput) returns (GetAllowanceOutput) { }

message GetAllowanceInput {
    string symbol = 1;
    aelf.Address owner = 2;
    aelf.Address spender = 3;
}

message GetAllowanceOutput {
    string symbol = 1;
    aelf.Address owner = 2;
    aelf.Address spender = 3;
    sint64 allowance = 4;
}
```

Input: 
- **symbol**
- **owner**
- **spender**

Output:
- **symbol**
- **owner**
- **balance**
- **spender**
- **allowance**

## **IsInWhiteList**

``` Proto
rpc IsInWhiteList (IsInWhiteListInput) returns (google.protobuf.BoolValue) { }

message IsInWhiteListInput {
    string symbol = 1;
    aelf.Address address = 2;
}
```

- **symbol**
- **address**

## **GetLockedAmount**

``` Proto
rpc GetLockedAmount (GetLockedAmountInput) returns (GetLockedAmountOutput) { }

message GetLockedAmountInput {
    aelf.Address address = 1;
    string symbol = 2;
    aelf.Hash lock_id = 3;
}

message GetLockedAmountOutput {
    aelf.Address address = 1;
    string symbol = 2;
    aelf.Hash lock_id = 3;
    sint64 amount = 4;
}
```
Input:
- **address**
- **symbol**
- **lock_id**

Output:
- **address**
- **symbol**
- **lock_id**
- **amount**

## **GetCrossChainTransferTokenContractAddress**

``` Proto
rpc GetCrossChainTransferTokenContractAddress (GetCrossChainTransferTokenContractAddressInput) returns (aelf.Address) { }

message GetCrossChainTransferTokenContractAddressInput {
    int32 chainId = 1;
}
```

- **chainId**

## **GetPrimaryTokenSymbol**

``` Proto
rpc GetPrimaryTokenSymbol (google.protobuf.Empty) returns (google.protobuf.StringValue) { 
```

Input

Output

## **GetCalculateFeeCoefficientOfContract**

``` Proto
rpc GetCalculateFeeCoefficientOfContract (aelf.SInt32Value) returns (CalculateFeeCoefficientsOfType) { }

message CalculateFeeCoefficientsOfType {
    repeated CalculateFeeCoefficient coefficients = 1;
}

message CalculateFeeCoefficient {
    sint32 piece_key = 1;
    FeeTypeEnum fee_type = 2;
    CalculateFunctionTypeEnum function_type = 3;
    map<string, sint32> coefficient_dic = 4;
}

enum CalculateFunctionTypeEnum {
    LINER = 0;
    POWER = 1;
}

enum FeeTypeEnum {
    READ = 0;
    STORAGE = 1;
    WRITE = 2;
    TRAFFIC = 3;
    TX = 4;
}
```

Input
- **coefficients**
  - **piece_key**
  - **fee_type**
  - **function_type**
  - **coefficient_dic**

Output

## **GetCalculateFeeCoefficientOfSender**

``` Proto
rpc GetCalculateFeeCoefficientOfSender (google.protobuf.Empty) returns (CalculateFeeCoefficientsOfType) { }

```

note: *for CalculateFeeCoefficientsOfType see GetCalculateFeeCoefficientOfContract*