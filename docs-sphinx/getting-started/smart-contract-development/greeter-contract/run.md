# Run the node

Next you can run Boilerplate (and it's an internal node). This will automatically deploy the Greeter contract. Open a terminal in the root Boilerplate directory and navigate to the launcher project:

```bash
cd chain/src/AElf.Boilerplate.GreeterContract.Launcher
```

Next, run the node:

```bash
dotnet run AElf.Boilerplate.GreeterContract.Launcher.csproj
```

From here, you should see the build and eventually the nodes logs.

Boilerplate will deploy your contract when the node starts. You can call the Boilerplate node API:

```bash
aelf-command get-chain-status
? Enter the the URI of an AElf node: http://127.0.0.1:1235
✔ Succeed
{
  "ChainId": "AELF",
  "Branches": {
    "6032b553ec9a5c81713cf8410f426dfc1ca0f43e64d56f527fc7a9c60b90e694": 3073
  },
  "NotLinkedBlocks": {},
  "LongestChainHeight": 3073,
  "LongestChainHash": "6032b553ec9a5c81713cf8410f426dfc1ca0f43e64d56f527fc7a9c60b90e694",
  "GenesisBlockHash": "c3bddca1909ebf37b95be7f26b990e07916790913e0f48da1a831b3c777d59ff",
  "GenesisContractAddress": "2gaQh4uxg6tzyH1ADLoDxvHA14FMpzEiMqsQ6sDG5iHT8cmjp8",
  "LastIrreversibleBlockHash": "85fee024d156de3be665c296c567423026e0e3369ad7dc5ee81dbb2a15dfe2f2",
  "LastIrreversibleBlockHeight": 3042,
  "BestChainHash": "6032b553ec9a5c81713cf8410f426dfc1ca0f43e64d56f527fc7a9c60b90e694",
  "BestChainHeight": 3073
}
```

This enables further testing of the contract, including testing it from a dApp.

## Next

We've just seen through this and the previous articles how to use Boilerplate in order to develop and test a smart contract. That said, these articles only show a subset of the possibilities. 

The next article will demonstrate how to build a small front-end for the greeter contract.

