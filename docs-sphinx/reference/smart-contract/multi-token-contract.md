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

```
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

The input type of `Transfer` method is:

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
// If this code has been executed since the contract deployment, then it can be skipped.
State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

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

Users can transfer tokens from one account to another account by calling the `TransferFrom` method.

Of course, the initiator of this transaction needs to obtain authorization to **from** account in advance via `Approve` method, 
we will discuss this authorization method later.

In the context of TransferFrom：
- **from** will be the transfer.
- **to** will be the token receiver.

The input type of `TransferFrom` method is:

```
message TransferFromInput {
    // The source address of the token.
    aelf.Address from = 1;
    // The destination address of the token.
    aelf.Address to = 2;
    // The symbol of the token to transfer.
    string symbol = 3;
    // The amount to transfer.
    int64 amount = 4;
    // The memo.
    string memo = 5;
}
```

If you have obtained an amount of approved value from the **"from"** account, 
you can send a `TransferFrom` transaction using the aelf-command tool.

```
aelf-command send AElf.ContractNames.Token TransferFrom '{"symbol": "ELF", "from": "C91b1SF5mMbenHZTfdfbJSkJcK7HMjeiuwfQu8qYjGsESanXR", "to": "2WNUjzNPyd7dBwo9a5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd", "amount": "1000000"}'
```

Of course, it should be noted that the transfer amount cannot exceed the approved amount.

When developing aelf smart contracts, it is often necessary to use the `TransferFrom` method to manipulate tokens:

```
// If this code has been executed since the contract deployment, then it can be skipped.
State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

State.TokenContract.TransferFrom.Send(new TransferFromInput
{
    Symbol = symbol,
    From = fromAddress,
    To = toAddress,
    Amount = amount
});
```

### Approve

Users can use this method to approve whether an amount of token from an account can be spent by a third-party account.

The `Approve` transaction's signer account will be the **"from"** account in the `TransferFrom` method.
And the "spender" account will have the right to transfer tokens from the **"from"** account.

The input type of `Approve` method is:

```
message ApproveInput {
    // The address that allowance will be increased. 
    aelf.Address spender = 1;
    // The symbol of token to approve.
    string symbol = 2;
    // The amount of token to approve.
    int64 amount = 3;
}
```

If you're using the aelf-command tool:

```
aelf-command send AElf.ContractNames.Token Approve '{"symbol": "ELF", "spender": "C91b1SF5mMbenHZTfdfbJSkJcK7HMjeiuwfQu8qYjGsESanXR", "amount": "1000000"}'
```

Next, you can use the `call` command of aelf-command tool to check the allowance via `GetAllowance` method.

The input type of `GetAllowance` method is:
```protobuf
message GetAllowanceInput {
    // The symbol of token.
    string symbol = 1;
    // The address of the token owner.
    aelf.Address owner = 2;
    // The address of the spender.
    aelf.Address spender = 3;
}
```

```
aelf-command call AElf.ContractNames.Token GetAllowance '{"symbol": "ELF", "owner": "2WNUjzNPyd7dBwo9a5KG56AXfPCdwmcydXv4kDyTyFZwTum5nd", "spender": "C91b1SF5mMbenHZTfdfbJSkJcK7HMjeiuwfQu8qYjGsESanXR"}'
```

### UnApprove

The **sponsor** (The **"from"** account in `TransferFrom` method) can use the `UnApprove` method to decrease the allowance of a **spender** account.

If the previous allowance was 100000, the sponsor can reduce the allowance by 50000 through the `UnApprove` method, then the allowance will be changed to 50000.

The input type of `UnApprove` method is:

```
message UnApproveInput {
    // The address that allowance will be decreased. 
    aelf.Address spender = 1;
    // The symbol of token to un-approve.
    string symbol = 2;
    // The amount of token to un-approve.
    int64 amount = 3;
}
```

If the `amount` filed of the input parameter exceeds or equals the allowance, then the allowance will become 0.

If you're using the aelf-command tool:

```
aelf-command send AElf.ContractNames.Token UnApprove '{"symbol": "ELF", "spender": "C91b1SF5mMbenHZTfdfbJSkJcK7HMjeiuwfQu8qYjGsESanXR", "amount": "1000000"}'
```

Remember to call the `GetAllowance` method to check the new allowance, ensure that the allowance has changed.

### Burn

If a token is burnable, then any token holder can burn their own tokens through `Burn` method.

Whether a token can be burned can be queried by calling `GetTokenInfo` method.

Like if you‘re trying to get the **TokenInfo** of **ELF**:

```
aelf-command call AElf.ContractNames.Token GetTokenInfo '{"symbol": "ELF"}'
```

The response will be :

```
AElf [Info]: 
Result:
{
  "symbol": "ELF",
  "tokenName": "Native Token",
  "supply": "99616865842269247",
  "totalSupply": "100000000000000000",
  "decimals": 8,
  "issuer": "cxZuMcWFE7we6CNESgh9Y4M6n7eM5JgFAgVMPrTNEv9wFEvQp",
  "isBurnable": true,
  "issueChainId": 9992731,
  "issued": "100000000000000000",
  "externalInfo": null,
  "owner": null
} 
```

Note this line: 
```
"isBurnable": true,
```
Which means the ELF token can be burned.

The input type of `Burn` method is:

```
message BurnInput {
    // The symbol of token to burn.
    string symbol = 1;
    // The amount of token to burn.
    int64 amount = 2;
}
```

If you're using the aelf-command tool:

```
aelf-command send AElf.ContractNames.Token Burn '{"symbol": "ELF", "amount": "100"}'
```

### Lock & Unlock

The `Lock` method is similar to `TransferFrom` in that 
`TransferFrom` directly transfers the tokens to a specific address, 
while `Lock` transfers the token to a virtual address generated by the MultiToken Contract.

This means that every time the `Lock` method is executed, a new virtual address will be generated to store the locked token for this time.
Moreover, only the MultiToken Contract have the permission to transfer tokens from this virtual address.

This to some extent ensures the security of user assets. 
The user only locks the tokens in a virtual address for some purpose, rather than directly sending it to another address; 
After meeting certain conditions, this tokens can be returned to the user.

The input type of `Lock` method is:
```
message LockInput {
    // The one want to lock his token.
    aelf.Address address = 1;
    // Id of the lock.
    aelf.Hash lock_id = 2;
    // The symbol of the token to lock.
    string symbol = 3;
    // a memo.
    string usage = 4;
    // The amount of tokens to lock.
    int64 amount = 5;
}
```

When you are an aelf smart contract developer and need users to lock tokens, this can be achieved through:

```
// If this code has been executed since the contract deployment, then it can be skipped.
State.TokenContract.Value = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

State.TokenContract.Lock.Send(new LockInput
{
    Symbol = YOUR_TOKEN_SYMBOL,
    Address = LOCKER_ADDRESS,
    Amount = AMOUNT,
    LockId = A_HASH_VALUE,
    Usage = MEMO
});
```

However, there is one condition for this operation: 
the address of the contract using this code, must be contained by the `lock_white_list` field of the locking token's `TokenInfo`.
Otherwise, this contract won't have the permission to lock user's certain type of tokens.

And to unlock tokens, the code will be like:
```
State.TokenContract.Unlock.Send(new UnlockInput
{
    Symbol = YOUR_TOKEN_SYMBOL,
    Address = LOCKER_ADDRESS,
    Amount = AMOUNT,
    LockId = SAME_HASH_VALUE
});
```

## How to customize token behaviour

If as a developer, you have already created your own token through the MultiToken Contract, 
then you can leverage `external_info` field of `TokenInfo` structure, to call a configured method (**Callback Method**) when the following actions occur on the token:

- Transfer / TransferFrom
- Lock
- Unlock

It should be noted that in the current version, 
`TokenInfo` can only be set when creating a new token.

The callback method looks like:

```
message CallbackInfo {
    aelf.Address contract_address = 1;
    string method_name = 2;
}
```

For example:
- You have already deployed a smart contract via Genesis Contract, 
the contract address is `2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS`.
- And there's a `CheckTransfer` method in your contract.
- You want to create an **ABC** token via the MultiToken Contract.

Let's say, if your intention is to automatically call the `CheckTransfer` method 
whenever someone transfers **ABC** tokens. If the method fails, the transfer will fail.

Then, when you create **ABC** token, 
you need to fill the **external_info** field with the key **aelf_transfer_callback**. The C# code will be like:

```
var externalInfo = new ExternalInfo();
externalInfo.Value.Add("aelf_transfer_callback", new CallbackInfo
{
    ContractAddress = Address.FromBase58("2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS"),
    MethodName = "CheckTransfer"
}.ToString());

State.TokenContract.Create.Send(new CreateInput
{
    Symbol = "ABC",
    TokenName = "ABC Token",
    Decimals = 8,
    Issuer = ISSUER_ADDRESS,
    Owner = OWNER_ADDRESS,
    IsBurnable = true,
    TotalSupply = 1000000000,
    ExternalInfo = externalInfo
});
```

In this way, whenever a user transfers **ABC** tokens in the future, 
the `CheckTransfer` method in the contract with address `2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS`
will be executed at the same time.
If the `CheckTransfer` method fails to execute, 
the `Transfer` or `TransferFrom` transaction will fail.

The author of aforementioned contract
can modify the implementation of the `CheckTransfer` method through contract update.

Similarly, key **aelf_lock_callback** can be used to specify the callback method when a user's ABC token is locked by the `Lock` method,
key **aelf_unlock_callback** can be used to specify the callback method when a user's ABC token is unlocked by the `Unlock` method.