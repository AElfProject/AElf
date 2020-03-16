# Front end

This tutorial will show you how to develop a front-end app (JavaScript in our case) that will demonstrate how to interact with a contract that was developed with Boilerplate. We will use an existing example in the Boilerplate repo: the Bingo contract.

## Structure

At the top level Boilerplate contains 2 folders:
- chain : used for developing the contracts.
- web : used for developing the front-end (or any client really).

We're interested here in the Bingo Game dApp, for which we have an exiting contract and front-end:
- [contract](https://github.com/AElfProject/aelf-boilerplate/tree/dev/chain/contract/AElf.Contracts.BingoGameContract)
- [front-end](https://github.com/AElfProject/aelf-boilerplate/tree/dev/web/browserBingo)

## Run the node

The first thing to do is run Boilerplate (and it's internal node). This will automatically deploy the Bingo Game contract. Open a terminal in the root Boilerplate directory and navigate to the launcher project:

```bash
cd chain/src/AElf.Boilerplate.Launcher
```

Next run the node:

```bash
dotnet run bin/Debug/netcoreapp3.1/AElf.Launcher.dll
```

From here you should see the build and eventually the nodes logs.

## Run the front-end

Open another terminal at the repos root and navigate to the `browserBingo' project:

```bash
cd web/browserBingo
```

From here, you can install and run the Bingo Game's front end:

```bash
npm i
npm start
```

and a page will be opened by webpack in your default browser.

## Front-end code

The code is straightforward, it uses aelf-sdk + webpack. You can check out more [**here**](https://github.com/AElfProject/aelf-sdk.js).