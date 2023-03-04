# aelf-sdk.js - AELF JavaScript API

## Introduction

aelf-sdk.js for aelf is like web.js for ethereum.

aelf-sdk.js is a collection of libraries which allow you to interact with a local or remote aelf node, using a HTTP connection.

The following documentation will guide you through installing and running aelf-sdk.js, as well as providing a API reference documentation with examples.

If you need more information you can check out the repo : [aelf-sdk.js](https://github.com/AElfProject/aelf-sdk.js)

## Adding aelf-sdk.js

First you need to get aelf-sdk.js into your project. This can be done using the following methods:

npm: `npm install aelf-sdk`

pure js: `link dist/aelf.umd.js`

After that you need to create a aelf instance and set a provider.

```js
// in brower use: <script src="https://unpkg.com/aelf-sdk@lastest/dist/aelf.umd.js"></script>
// in node.js use: const AElf = require('aelf-sdk');
const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:8000'));
```

## Examples

You can also see full examples in `./examples`;

### Create instance

Create a new instance of AElf, connect to an AELF chain node.

```javascript
    import AElf from 'aelf-sdk';

    // create a new instance of AElf
    const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1235'));
```

### Create or load a wallet

Create or load a wallet with `AElf.wallet`

    ```javascript
    // create a new wallet
    const newWallet = AElf.wallet.createNewWallet();
    // load a wallet by private key
    const priviteKeyWallet = AElf.wallet.getWalletByPrivateKey('xxxxxxx');
    // load a wallet by mnemonic
    const mnemonicWallet = AElf.wallet.getWalletByMnemonic('set kite ...');
    ```

### 3.Get a system contract address

Get a system contract address, take `AElf.ContractNames.Token` as an example

```javascript
    const tokenContractName = 'AElf.ContractNames.Token';
    let tokenContractAddress;
    (async () => {
      // get chain status
      const chainStatus = await aelf.chain.getChainStatus();
      // get genesis contract address
      const GenesisContractAddress = chainStatus.GenesisContractAddress;
      // get genesis contract instance
      const zeroContract = await aelf.chain.contractAt(GenesisContractAddress, newWallet);
      // Get contract address by the read only method `GetContractAddressByName` of genesis contract
      tokenContractAddress = await zeroContract.GetContractAddressByName.call(AElf.utils.sha256(tokenContractName));
    })()
```

### 4.Get a contract instance

Get a contract instance by contract address

```javascript
    const wallet = AElf.wallet.createNewWallet();
    let tokenContract;
    // Use token contract for examples to demonstrate how to get a contract instance in different ways
    // in async function
    (async () => {
      tokenContract = await aelf.chain.contractAt(tokenContractAddress, wallet)
    })();

    // promise way
    aelf.chain.contractAt(tokenContractAddress, wallet)
      .then(result => {
        tokenContract = result;
      });

    // callback way
    aelf.chain.contractAt(tokenContractAddress, wallet, (error, result) => {if (error) throw error; tokenContract = result;});

```

### 5.Use contract instance

How to use contract instance

    A contract instance consists of several contract methods and methods can be called in two ways: read-only and send transaction.

```javascript
    (async () => {
      // get the balance of an address, this would not send a transaction,
      // or store any data on the chain, or required any transaction fee, only get the balance
      // with `.call` method, `aelf-sdk` will only call read-only method
      const result = await tokenContract.GetBalance.call({
        symbol: "ELF",
        owner: "7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz"
      });
      console.log(result);
      /**
      {
        "symbol": "ELF",
        "owner": "2661mQaaPnzLCoqXPeys3Vzf2wtGM1kSrqVBgNY4JUaGBxEsX8",
        "balance": "1000000000000"
      }*/
      // with no `.call`, `aelf-sdk` will sign and send a transaction to the chain, and return a transaction id.
      // make sure you have enough transaction fee `ELF` in your wallet
      const transactionId = await tokenContract.Transfer({
        symbol: "ELF",
        to: "7s4XoUHfPuqoZAwnTV7pHWZAaivMiL8aZrDSnY9brE1woa8vz",
        amount: "1000000000",
        memo: "transfer in demo"
      });
      console.log(transactionId);
      /**
        {
          "TransactionId": "123123"
        }
      */
    })()
```

### 6.Change the node endpoint

Change the node endpoint by using `aelf.setProvider`

    ```javascript
    import AElf from 'aelf-sdk';

    const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1235'));
    aelf.setProvider(new AElf.providers.HttpProvider('http://127.0.0.1:8000'));
    ```

## Web API

*You can see how the Web Api of the node works in `{chainAddress}/swagger/index.html`*
_tip: for an example, my local address: 'http://127.0.0.1:1235/swagger/index.html'_

parameters and returns based on the URL: `https://aelf-public-node.aelf.io/swagger/index.html`

The usage of these methods is based on the AElf instance, so if you don't have one please create it:

```javascript
import AElf from 'aelf-sdk';

// create a new instance of AElf, change the URL if needed
const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1235'));
```

### 1.getChainStatus

Get the current status of the block chain.

_Web API path_

`/api/blockChain/chainStatus`

_Parameters_

Empty

_Returns_

`Object`

- `ChainId - String`
- `Branches - Object`
- `NotLinkedBlocks - Object`
- `LongestChainHeight - Number`
- `LongestChainHash - String`
- `GenesisBlockHash - String`
- `GenesisContractAddress - String`
- `LastIrreversibleBlockHash - String`
- `LastIrreversibleBlockHeight - Number`
- `BestChainHash - String`
- `BestChainHeight - Number`


_Example_

```javascript
aelf.chain.getChainStatus()
.then(res => {
  console.log(res);
})
```

### 2.getContractFileDescriptorSet

Get the protobuf definitions related to a contract

_Web API path_

`/api/blockChain/contractFileDescriptorSet`

_Parameters_
1. `contractAddress - String` address of a contract

_Returns_

`String`

_Example_
```javascript
aelf.chain.getContractFileDescriptorSet(contractAddress)
  .then(res => {
    console.log(res);
  })
```

### 3.getBlockHeight

Get current best height of the chain.

_Web API path_

`/api/blockChain/blockHeight`

_Parameters_

Empty

_Returns_

`Number`

_Example_
```javascript
aelf.chain.getBlockHeight()
  .then(res => {
    console.log(res);
  })
```

### 4.getBlock

Get block information by block hash.

_Web API path_

`/api/blockChain/block`

_Parameters_

1. `blockHash - String`
2. `includeTransactions - Boolean` :
  - `true` require transaction ids list in the block
  - `false` Doesn't require transaction ids list in the block

_Returns_

`Object`

- `BlockHash - String`
- `Header - Object`
  - `PreviousBlockHash - String`
  - `MerkleTreeRootOfTransactions - String`
  - `MerkleTreeRootOfWorldState - String`
  - `Extra - Array`
  - `Height - Number`
  - `Time - google.protobuf.Timestamp`
  - `ChainId - String`
  - `Bloom - String`
  - `SignerPubkey - String`
- `Body - Object`
  - `TransactionsCount - Number`
  - `Transactions - Array`
    - `transactionId - String`

_Example_
```javascript
aelf.chain.getBlock(blockHash, false)
  .then(res => {
    console.log(res);
  })
```

### 5.getBlockByHeight

_Web API path_

`/api/blockChain/blockByHeight`

Get block information by block height.

_Parameters_

1. `blockHeight - Number`
2. `includeTransactions - Boolean` :
  - `true` require transaction ids list in the block
  - `false` Doesn't require transaction ids list in the block

_Returns_

`Object`

- `BlockHash - String`
- `Header - Object`
  - `PreviousBlockHash - String`
  - `MerkleTreeRootOfTransactions - String`
  - `MerkleTreeRootOfWorldState - String`
  - `Extra - Array`
  - `Height - Number`
  - `Time - google.protobuf.Timestamp`
  - `ChainId - String`
  - `Bloom - String`
  - `SignerPubkey - String`
- `Body - Object`
  - `TransactionsCount - Number`
  - `Transactions - Array`
    - `transactionId - String`

_Example_
```javascript
aelf.chain.getBlockByHeight(12, false)
  .then(res => {
    console.log(res);
  })
```

### 6.getTxResult

Get the result of a transaction

_Web API path_

`/api/blockChain/transactionResult`

_Parameters_

1. `transactionId - String`

_Returns_

`Object`

- `TransactionId - String`
- `Status - String`
- `Logs - Array`
  - `Address - String`
  - `Name - String`
  - `Indexed - Array`
  - `NonIndexed - String`
- `Bloom - String`
- `BlockNumber - Number`
- `Transaction - Object`
  - `From - String`
  - `To - String`
  - `RefBlockNumber - Number`
  - `RefBlockPrefix - String`
  - `MethodName - String`
  - `Params - Object`
  - `Signature - String`
- `ReadableReturnValue - Object`
- `Error - String`

_Example_
```javascript
aelf.chain.getTxResult(transactionId)
  .then(res => {
    console.log(res);
  })
```

### 7.getTxResults

Get multiple transaction results in a block

_Web API path_

`/api/blockChain/transactionResults`

_Parameters_

1. `blockHash - String`
2. `offset - Number`
3. `limit - Number`

_Returns_
  `Array` - The array of method descriptions:
  - the transaction result object

_Example_
```javascript
aelf.chain.getTxResults(blockHash, 0, 2)
  .then(res => {
    console.log(res);
  })
```

### 8.getTransactionPoolStatus

Get the transaction pool status.

_Web API path_

`/api/blockChain/transactionPoolStatus`

_Parameters_

Empty

### 9.sendTransaction

Broadcast a transaction

_Web API path_

`/api/blockChain/sendTransaction`

_POST_

_Parameters_

`Object` - Serialization of data into protobuf data, The object with the following structure :
- `RawTransaction - String` :

usually developers don't need to use this function directly, just get a contract method and send transaction by call contract method:

### 10.sendTransactions

Broadcast multiple transactions

_POST_

_Parameters_

`Object` - The object with the following structure :
- `RawTransaction - String`

### 11.callReadOnly

Call a read-only method on a contract.

_POST_

_Parameters_

`Object` - The object with the following structure :
- `RawTransaction - String`

### 12.getPeers

Get peer info about the connected network nodes

_GET_

_Parameters_

1. `withMetrics - Boolean` :

- `true` with metrics
- `false` without metrics

### 13.addPeer

Attempts to add a node to the connected network nodes

_POST_

_Parameters_

`Object` - The object with the following structure :

- `Address - String`

### 14.removePeer

Attempts to remove a node from the connected network nodes

_DELETE_

_Parameters_

1. `address - String`

### 15.calculateTransactionFee

Estimate transaction fee

_POST_

_Parameters_

`Object` - The object with the following structure :

- `RawTransaction - String`

### 16.networkInfo

Get information about the node’s connection to the network

_GET_

_Parameters_

Empty

## AElf.wallet

`AElf.wallet` is a static property of `AElf`.

_Use the api to see detailed results_

### 1.createNewWallet

_Returns_

`Object`

- `mnemonic - String`: mnemonic
- `BIP44Path - String`: m/purpose'/coin_type'/account'/change/address_index
- `childWallet - Object`: HD Wallet
- `keyPair - String`: The EC key pair generated by elliptic
- `privateKey - String`: private Key
- `address - String`: address

_Example_
```javascript
import AElf from 'aelf-sdk';
const wallet = AElf.wallet.createNewWallet();
```

### 2.getWalletByMnemonic

_Parameters_

1. `mnemonic - String` : wallet's mnemonic

_Returns_

`Object`: Complete wallet object.

_Example_
```javascript
const wallet = AElf.wallet.getWalletByMnemonic(mnemonic);
```

### 3.getWalletByPrivateKey

_Parameters_

1.  `privateKey: String` : wallet's private key

_Returns_

`Object`: Complete wallet object, with empty mnemonic

_Example_
```javascript
const wallet = AElf.wallet.getWalletByPrivateKey(privateKey);
```

### 4.signTransaction

Use wallet `keypair` to sign a transaction

_Parameters_
1. `rawTxn - String`
2. `keyPair - String`

_Returns_

`Object`: The object with the following structure :

_Example_
```javascript
const result = aelf.wallet.signTransaction(rawTxn, keyPair);
```

### 5.AESEncrypt

Encrypt a string by aes algorithm

_Parameters_

1. `input - String`
2. `password - String`

_Returns_

`String`

### 6.AESDecrypt

Decrypt by aes algorithm

_Parameters_

1. `input - String`
2. `password - String`

_Returns_

`String`

## AElf.pbjs

The reference to protobuf.js, read the [documentation](https://github.com/protobufjs/protobuf.js) to see how to use.

## AElf.pbUtils

Some basic format methods of aelf.

For more information, please see the code in `src/utils/proto.js`. It is simple and easy to understand.

### AElf.utils

Some methods for aelf.

For more information, please see the code in `src/utils/utils.js`. It is simple and easy to understand.

#### Check address

```javascript
const AElf = require('aelf-sdk');
const {base58} = AElf.utils;
base58.decode('$addresss'); // throw error if invalid
```

## AElf.version

```javascript
import AElf from 'aelf-sdk';
AElf.version // eg. 3.2.23
```

## Requirements

- [Node.js](https://nodejs.org)
- [NPM](http://npmjs.com/)

## Support

![browsers](https://img.shields.io/badge/browsers-latest%202%20versions-brightgreen.svg)
![node](https://img.shields.io/badge/node->=10-green.svg)

## About contributing

Read out [contributing guide]

## About Version

https://semver.org/
