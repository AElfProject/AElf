# aelf-sdk.js - AELF JavaScript API

[![Build Status][1]][2]

[1]: https://travis-ci.org/AElfProject/aelf-sdk.js.svg?branch=master
[2]: https://travis-ci.org/AElfProject/aelf-sdk.js


## Introduction

This is the AELF JavaScript API which connects to the Generic JSON RPC spec.

You need to run a local or remote AELF node to use this library.

If you need more information you can check out the repo : [aelf-sdk.js](https://github.com/AElfProject/aelf-sdk.js)

## Installation

### Node

```js
npm install aelf-sdk
```

### Yarn

```js
yarn add aelf-sdk
```

## Usage

Five modules.

```js
Aelf, Aelf.wallet, Aelf.pbjs, Aelf.pbUtils, Aelf.version
```

### basic

```js
import Aelf from 'aelf-sdk';

// host, timeout, user, password, headers
const aelf = new Aelf(
    new Aelf.providers.HttpProvider(
        host, // https://127.0.0.1:8000/chain
        timeout, // 300
        user, // hzz780
        password, // passowrd
        // header
        [{
            name: 'x-csrf-token',
            value: document.cookie.match(/csrfToken=[^;]*/)[0].replace('csrfToken=', '')
        }]
    )
);
```

init contract and call methods

```js
// contractAddress = xxx; wallet = xxx;
// We use token contract for example.
aelf.chain.contractAtAsync(contractAddress, wallet, (err, result) => {
    const contractoktMethods = result;
    // contractMethods.methodName(param01, ..., paramN, callback);
    // contractMethods.methodName.call(param01, ..., paramN, callback);
    contractoktMethods.Transfer({
        symbol: 'ELF',
        to: '58h3RwTfaE8RDpRNMAMiMv8jUjanCeYHBzKuQfHbrfSFTCn',
        amount: '1000'
    }, (err, result) => {
    });

    // will not send transaction when use .call
    contractMethods.GetBalance.call({
        symbol: 'ELF',
        owner: '58h3RwTfaE8RDpRNMAMiMv8jUjanCeYHBzKuQfHbrfSFTCn'
    }, (err, result) => {
    });
});
```

Additionally you can set a provider using aelf.setProvider()

```js
import Aelf from 'aelf-sdk';

const aelf = new Aelf(new Aelf.providers.HttpProvider('https://127.0.0.1:8000/chain'));
aelf.setProvider(new Aelf.providers.HttpProvider('https://127.0.0.1:8010/chain'));
```

### wallet

base on bip39.

```js
import Aelf from 'aelf-sdk';

Aelf.wallet.createNewWallet();
// wallet.AESDecrypto            wallet.AESEncrypto            wallet.bip39
// wallet.createNewWallet        wallet.getWalletByMnemonic    wallet.getWalletByPrivateKey
// wallet.sign                   wallet.signTransaction
```

### pbjs

almost the same as protobufjs

Sometimes we have to deal with some protobuf data.

### pbUtils

Some basic format methods of aelf.

For more information, please see the code in ./lib/aelf/proto.js. It is simple and easy to understand.

```js
    // methods.
    getRepForAddress
    getAddressFromRep
    getAddressObjectFromRep
    getRepForHash
    getHashFromHex
    getHashObjectFromHex
    getTransaction
    getMsigTransaction
    getAuthorization
    getReviewer
    encodeTransaction
    getProposal
    encodeProposal
    getApproval
    encodeApproval
    getSideChainInfo
    getBalance
    encodeSideChainInfo
    Transaction
    Hash
    Address
    Authorization
    Proposal
    ProposalStatus
    SideChainInfo
    SideChainStatus
    ResourceTypeBalancePair
```

### version

```js
import Aelf from 'aelf-sdk';
Aelf.version // eg. 2.1.10
```

## Contributing

- All contributions have to go into the dev-2.0 branch

- Please follow the code style of the other files, we use 4 spaces as tabs.

### Requirements

- [Node.js](https://nodejs.org)

- npm

### Support

![browsers](https://img.shields.io/badge/browsers-latest%202%20versions-brightgreen.svg)
![node](https://img.shields.io/badge/node->=6-green.svg)

## Somthing more

### pbjs

#### how to use pbjs convert proto to json

node ./node_modules/protobufjs/bin/pbjs -t json ./lib/aelf/proto/abi.proto > ./lib/aelf/proto/abi.proto.json

## About Version

https://semver.org/
