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

- Basic operations on tokens
- How to create and issue a new type of token
- How to customize token behaviour

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
For example, if you use `aelf-command` tool described [here](https://aelf-ean.readthedocs.io/en/latest/reference/cli/index.html):
```Bash
aelf-command send AElf.ContractNames.Token Transfer '{"symbol": "ELF", "to": "C91b1SF5mMbenHZTfdfbJSkJcK7HMjeiuwfQu8qYjGsESanXR", "amount": "1000000"}'
```

