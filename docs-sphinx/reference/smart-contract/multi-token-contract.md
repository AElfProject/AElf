# MultiToken Contract

## Overview

In the aelf blockchain, native token **ELF** is issued, 
managed, and circulated through the MultiToken Contract.

We have implemented all the functions defined by ERC20 in the MultiToken contract:

- transfer (Transfer)
- transferFrom (TransferFrom)
- approve (Approve / UnApprove)
- balanceOf (GetBalance)
- allowance (GetAllowance)
- name, symbol, decimals, totalSupply (GetTokenInfo)
- burn (Burn)

Cross chain transfer between aelf MainChain and SideChains is also achieved through the MultiToken Contract,
refer to [CrossChain Contract](https://aelf-ean.readthedocs.io/en/latest/reference/smart-contract/cross-chain-contract.html) 
for more details because we won't talk too much about this topic in this article:

- CrossChainTransfer
- CrossChainReceiveToken

On the basis of implementing the above methods, 
all users can create their own tokens on the MultiToken contract, 
without having to write their own code.

- Create
- Issue

Once a new token is created in the MultiToken Contract, it can automatically equip the functions defined in ERC20, just like **ELF**, and allow this type of token to be transferred across the MainChain and SideChains of aelf. 
At the same time, we have reserved some customized solutions to provide the possibility of customizing token operations.

In addition, we have also provided more operability for tokens hosted in the MultiToken Contract, such as Lock and UnLock.

Due to the fact that aelf's smart contracts can be updated after deployment, in order to ensure users' assets, we recommend all developers to use MultiToken Contract to create and manage tokens.

In this article, we will discuss:

- How to create and issue a new type of token
- Basic operations on tokens
- How to customize token behaviour

## Create and Issue new type of token

### Create

Any ELF token holder can create their own token in the MultiToken Contract through the `Create` method
on the aelf MainChain after paying an amount of transaction fees.

Note: Token creation can **only** be happened in the aelf **MainChain**.

The input type of `Create` method is:

```
message CreateInput {
    // The symbol of the token.
    string symbol = 1;
    // The full name of the token.
    string token_name = 2;
    // The total supply of the token.
    int64 total_supply = 3;
    // The precision of the token
    int32 decimals = 4;
    // The address that has permission to issue the token.
    aelf.Address issuer = 5;
    // A flag indicating if this token is burnable.
    bool is_burnable = 6;
    // A whitelist address list used to lock tokens.
    repeated aelf.Address lock_white_list = 7;
    // The chain id of the token.
    int32 issue_chain_id = 8;
    // The external information of the token.
    ExternalInfo external_info = 9;
    // The address that owns the token.
    aelf.Address owner = 10;
}
```

For example, if you use **aelf-command** tool described [here](https://aelf-ean.readthedocs.io/en/latest/reference/cli/index.html), 
this command will help you create a new type of token:

```
aelf-command send AElf.ContractNames.Token Create '{"symbol": "NEW_TOKEN_SYMBOL", "tokenName": "New Token Name", "totalSupply": "1000000000", "decimals": "8"}'
```

After the new token is successfully created, users can obtain relevant information about the token they have created through the `GetTokenInfo` method.

The input type of `GetTokenInfo` method is:

```
message GetTokenInfoInput {
    // The symbol of token.
    string symbol = 1;
}
```

You can use the `call` command of **aelf-command** tool to get the token information:

```protobuf
aelf-command call AElf.ContractNames.Token GetTokenInfo '{"symbol": "NEW_TOKEN_SYMBOL"}'
```

For instance, to get the token information of **ELF**, the interaction will be like:

```
aelf-command call AElf.ContractNames.Token GetTokenInfo '{"symbol": "ELF"}'             

? Enter the the URI of an AElf node: https://aelf-public-node.aelf.io
? Enter a valid wallet address, if you don't have, create one by aelf-command cr
eate: 2WNUjzNPyd7dBwo9a5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd
? Enter the password you typed when creating a wallet: ********
✔ Fetching contract successfully!
✔ Calling method successfully!
AElf [Info]: 
Result:
{
  "symbol": "ELF",
  "tokenName": "Native Token",
  "supply": "99616840732317872",
  "totalSupply": "100000000000000000",
  "decimals": 8,
  "issuer": "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
  "isBurnable": true,
  "issueChainId": 9992731,
  "issued": "100000000000000000",
  "externalInfo": null,
  "owner": null
} 
✔ Succeed!
```

### Issue

When the user successfully creates a new token, 
the token is still in a non circulating state. 
Token circulation can be achieved by calling the `Issue` method.

The input type of `Issue` method is:

```protobuf
message IssueInput {
    // The token symbol to issue.
    string symbol = 1;
    // The token amount to issue.
    int64 amount = 2;
    // The memo.
    string memo = 3;
    // The target address to issue.
    aelf.Address to = 4;
}
```

You can use the `send` command of **aelf-command** tool complete the issue process:

```
aelf-command send AElf.ContractNames.Token Issue '{"symbol": "NEW_TOKEN_SYMBOL", "amount": "1000000000", "to": "2WNUjzNPyd7dBwo9a5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd"}'
```

Then the address `2WNUjzNPyd7dBwo9a5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd` will receive 1000000000 new tokens.

Next, you can use the `call` command of aelf-command tool to check the balance via `GetBalance` method.

The input type of `GetBalance` method is:
```protobuf
message GetBalanceInput {
    // The symbol of token.
    string symbol = 1;
    // The target address of the query.
    aelf.Address owner = 2;
}
```

```
aelf-command call AElf.ContractNames.Token GetBalance '{"symbol": "NEW_TOKEN_SYMBOL", "owner": "2WNUjzNPyd7dBwo9a5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd"}'
```

In addition, if developers wish to place the logic of the token issue in their own smart contract, 
they can place the **Issue** operation in an appropriate position in the code through cross contract calls.

```
// If this code has been executed since the contract deployment, then it can be skipped.
State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

State.TokenContract.Issue.Send(new IssueInput
{
    Symbol = symbol,
    To = receiverAddress,
    Amount = amount
});
```

or:

```
var tokenContractAddress = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
Context.SendInline(tokenContractAddress, "Issue", new IssueInput
{
    Symbol = symbol,
    To = receiverAddress,
    Amount = amount
});
```

### Create NFT

Users can also create NFTs directly through the MultiToken Contract.

The creation of NFTs can be divided into two categories: NFT Collection and a new type of NFT.
This difference is distinguished by the `symbol` filed when users send the `Create` transaction.

If the `symbol` filed contains a **"-"** character, it will be considered as creating a NFT Collection or a new type of NFT.
And if there is a **"0"** after the **"-"** character, it will be considered as creating a NFT Collection.
Otherwise, it will be considered as creating a new type of NFT.

Only after creating a NFT Collection can new NFTs be created in that Collection.

When creating a NFT Collection, you can specify an image through the URL to facilitate displaying the NFT Collection in other tools on aelf.
The URL should be put the `external_info` filed via a specific key: **__nft_image_url**.

For example, developers can use the following code to create a NFT Collection named **HELLO** in their contract code:

```
State.TokenContract.Create.Send(new CreateInput
{
    Symbol = "HELLO-0",
    TokenName = "Hello",
    TotalSupply = 1000,
    Decimals = 0,//nft decimal=0
    Issuer = issuerAddress,
    IssueChainId = chainId,
    ExternalInfo = new ExternalInfo()
    {
        Value =
        {
            {
                "__nft_image_url",
                "https://example.com/head.jpg"
            }
        }
    }
})；
```

Next, through the same `Create` method, users can create NFTs for the "Hello" NFT Collection.

```
State.TokenContract.Create.Send(new CreateInput
{
    Symbol = "HELLO-0001",
    TokenName = "Hello",
    TotalSupply = 10,
    Decimals = 0,
    Issuer = issuerAddress,
    IssueChainId = chainId,
    ExternalInfo = new ExternalInfo()
    {
        Value =
        {
            {
                "__nft_image_url",
                "https://example.com/1.jpg"
            }
        }
    }
})；
```

The issuance, transfer, and cross-chain transfer of NFTs are consistent with FTs,
so there will be no further explanation here.

## Basic operations on tokens

### Transfer

Users can transfer their own tokens by calling the Transfer method.

The input type is:

```protobuf
message TransferInput {
    // The receiver of the token.
    aelf.Address to = 1;
    // The token symbol to transfer.
    string symbol = 2;
    // The amount to to transfer.
    int64 amount = 3;
    // The memo.
    string memo = 4;
}
```

For example, if you use `aelf-command` tool described [here](https://aelf-ean.readthedocs.io/en/latest/reference/cli/index.html), this command will help you transfer your tokens:

```
aelf-command send AElf.ContractNames.Token Transfer '{"symbol": "ELF", "to": "C91b1SF5mMbenHZTfdfbJSkJcK7HMjeiuwfQu8qYjGsESanXR", "amount": "1000000"}'
```

If you're developing an aelf contract, after initializing the reference contract stub `State.TokenContract.Value` (by setting its address), you can do this:

```
State.TokenContract.Transfer.Send(new TransferInput
{
    Symbol = symbol,
    To = receiverAddress,
    Amount = amount
});
```

This will transfer tokens **from your contract** to the account of `receiverAddress`.

If your intention was not to transfer tokens from your contract, then you may consider to use `TransferFrom` method.

### TransferFrom

Users can transfer tokens from one account to another account by calling the TransferFrom method.

Of course, the initiator of this transaction needs to obtain authorization to **from** account in advance, 
and the authorization method will be discussed later.

