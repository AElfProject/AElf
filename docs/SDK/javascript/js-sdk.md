# aelf-sdk.js - AELF JavaScript API

For dApp developpers we have a javascript sdk, to help interaction with the JSON RPC exposed by the node. Of course, you need to run a an AELF node to use the sdk.
If you need more information you can check out the repo : [aelf-sdk.js](https://github.com/AElfProject/aelf-sdk.js)

You can find 

## Usage

```js
AElf, AElf.wallet, AElf.pbjs, AElf.pbUtils, AElf.version
```

### basic

```js
import AElf from 'aelf-sdk';

// host, timeout, user, password, headers
const aelf = new AElf(
    new AElf.providers.HttpProvider(
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
import AElf from 'aelf-sdk';

const aelf = new AElf(new AElf.providers.HttpProvider('https://127.0.0.1:8000/chain'));
aelf.setProvider(new AElf.providers.HttpProvider('https://127.0.0.1:8010/chain'));
```

### wallet

base on bip39.

```js
import AElf from 'aelf-sdk';

AElf.wallet.createNewWallet();
// wallet.AESDecrypto            wallet.AESEncrypto            wallet.bip39
// wallet.createNewWallet        wallet.getWalletByMnemonic    wallet.getWalletByPrivateKey
// wallet.sign                   wallet.signTransaction
```

### pbjs

Almost the same as protobufjs, sometimes we have to deal with some protobuf data.

### pbUtils

Some basic methods ofthe sdk AElf. For more information, please see the code in /lib/aelf/proto.js. It is simple and easy to understand.

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
import AElf from 'aelf-sdk';
AElf.version // eg. 2.1.10
```

## Contributing

- All contributions have to go into the dev-2.0 branch
- Please follow the code style of the other files, we use 4 spaces as tabs.

### Requirements

- [Node.js](https://nodejs.org)
- npm

