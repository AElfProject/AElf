# aelf-web-extension

## For User

[release version, please waiting](#)

[dev version](https://chrome.google.com/webstore/detail/aelf-explorer-extension-d/mlmlhipeonlflbcclinpbmcjdnpnmkpf)

If you are using qq browser,etc, you can add the extention too.

### Notice

```note
Using File:/// protocol may can not use the extenstion
// https://developer.chrome.com/extensions/match_patterns
Note: Access to file URLs isn't automatic. The user must visit the extensions management page and opt in to file access for each extension that requests it.
```

## For Dapp Developers

[Token Demo](https://github.com/hzz780/aelf-web-extension/tree/master/demo/token)

### How to use

If you need complete data structure. you can [click here](#dataformat)

- [1. GET_CHAIN_INFORMATION](#get-chain-information)
- [2. CALL_AELF_CHAIN](#call-aelf-chain)
- [3. LOGIN](#login)
- [4. INIT_AELF_CONTRACT](#init-aelf-contract)
- [5. CALL_AELF_CONTRACT / CALL_AELF_CONTRACT_READONLY](#call-aelf-contract)
- [6. CHECK_PERMISSION](#check-permission)
- [7. GET_ADDRESS](#get-address)
- [8. SET_CONTRACT_PERMISSION](#set-contract-permission)
- [9. REMOVE_CONTRACT_PERMISSION](#remove-contract-permission)
- [10. REMOVE_METHODS_WHITELIST](#remove-methods-whitelist)

<span id="dataformat"></span>

## Data Format

```javascript
    NightElf = {
        histories: [],
        keychain: {
            keypairs: [
                {
                    name: 'your keypairs name',
                    address: 'your keypairs address',
                    mnemonic: 'your keypairs mnemonic',
                    privateKey: 'your keupairs privateKey',
                    publicKey: {
                        x: 'you keupairs publicKey',
                        y: 'you keupairs publicKey'
                    }
                }
            ],
            permissions: [
                {
                    chainId: 'AELF',
                    contractAddress: 'contract address',
                    contractName: 'contract name',
                    description: 'contract description',
                    github: 'contract github',
                    whitelist: {
                        Approve: {
                            parameter1: 'a',
                            parameter2: 'b',
                            parameter3: 'c'
                        }
                    }
                }
            ]
        }
    }
```

<span id="get-chain-information"></span>

### 1.GET_CHAIN_INFORMATION

You can see the demo [./devDemos/test.html](https://github.com/hzz780/aelf-web-extension/tree/1.0/devDemos). [demo.js just a draft]

If you want to check Token Transfer Demo.
You can [click here](https://github.com/hzz780/aelf-web-extension/tree/master/demo/token)

The methods calls act the same as the methods call of the aelf-sdk.js

Note: ``` '...' ``` stands for omitted data.

```javascript
const aelf = new window.NightElf.AElf({
    httpProvider: 'http://192.168.199.210:5000/chain',
    appName: 'Test'
});

aelf.chain.getChainInformation((error, result) => {
    console.log('>>>>>>>>>>>>> connectChain >>>>>>>>>>>>>');
    console.log(error, result);
});

// result = {
//     ChainId: "AELF"
//     GenesisContractAddress: "61W3AF3Voud7cLY2mejzRuZ4WEN8mrDMioA9kZv3H8taKxF"
// }
```

<span id="call-aelf-chain"></span>

### 2.CALL_AELF_CHAIN

```javascript
const txid = 'c45edfcca86f4f528cd8e30634fa4ac53801aae05365cfefc3bfe9b652fe5768';
aelf.chain.getTxResult(txid, (err, result) => {
    console.log('>>>>>>>>>>>>> getTxResult >>>>>>>>>>>>>');
    console.log(err, result);
});

// result = {
//     Status: "NotExisted"
//     TransactionId: "ff5bcd126f9b7f22bbfd0816324390776f10ccb3fe0690efc84c5fcf6bdd3fc6"
// }
```

<span id="login"></span>

### 3. LOGIN

```javascript
aelf.login({
    appName: 'hzzTest',
    chainId: 'AELF',
    payload: {
        method: 'LOGIN',
        contracts: [{
            chainId: 'AELF',
            contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
            contractName: 'token',
            description: 'token contract',
            github: ''
        }, {
            chainId: 'AELF TEST',
            contractAddress: '2Xg2HKh8vusnFMQsHCXW1q3vys5JxG5ZnjiGwNDLrrpb9Mb',
            contractName: 'TEST contractName',
            description: 'contract description',
            github: ''
        }]
    }
}, (error, result) => {
    console.log('login>>>>>>>>>>>>>>>>>>', result);
});

// keychain = {
//     keypairs: {
//         name: 'your keypairs name',
//         address: 'your keypairs address',
//         mnemonic: 'your keypairs mnemonic',
//         privateKey: 'your keypairs privateKey'，
//         publicKey: {
//             x: 'f79c25eb......',
//             y: '7fa959ed......'
//         }
//     },
//     permissions: [{
//         appName: 'hzzTest',
//         address: 'your keyparis address',
//         contracts: [{
//             chainId: 'AELF',
//             contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
//             contractName: 'token',
//             description: 'token contract',
//             github: ''
//         }],
//         domain: 'Dapp domain'
//     }]
// }
```

<span id="init-aelf-contract"></span>

### 4.INIT_AELF_CONTRACT

```javascript
// In aelf-sdk.js wallet is the realy wallet.
// But in extension sdk, we just need the address of the wallet.
const tokenContract;
const wallet = {
    address: '2JqnxvDiMNzbSgme2oxpqUFpUYfMjTpNBGCLP2CsWjpbHdu'
};
// It is different from the wallet created by Aelf.wallet.getWalletByPrivateKey();
// There is only one value named address;
aelf.chain.contractAtAsync(
    '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
    wallet,
    (error, result) => {
        console.log('>>>>>>>>>>>>> contractAtAsync >>>>>>>>>>>>>');
        console.log(error, result);
        tokenContract = result;
    }
);

// result = {
//     Approve: ƒ (),
//     Burn: ƒ (),
//     ChargeTransactionFees: ƒ (),
//     ClaimTransactionFees: ƒ (),
//     ....
// }
```

<span id="call-aelf-contract"></span>

### 5.CALL_AELF_CONTRACT / CALL_AELF_CONTRACT_READONLY

```javascript
// tokenContract from the contractAsync
tokenContract.GetBalance.call(
    {
        symbol: 'AELF',
        owner: '65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9'
    },
    (err, result) => {
        console.log('>>>>>>>>>>>>>>>>>>>', result);
    }
);

tokenContract.Approve(
    {
        symbol: 'AELF',
        spender: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
        amount: '100'
    },
    (err, result) => {
        console.log('>>>>>>>>>>>>>>>>>>>', result);
    }
);

// If you use tokenContract.GetBalance.call  this method is only applicable to queries that do not require extended authorization validation.(CALL_AELF_CONTRACT_READONLY)
// If you use tokenContract.Approve this requires extended authorization validation (CALL_AELF_CONTRACT)

// tokenContract.GetBalance.call(payload, (error, result) => {})
// result = {
//     symbol: "AELF",
//     owner: "65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9",
//     balance: 0
// }
```

<span id="check-permission"></span>

### 6.CHECK_PERMISSION

```javascript
aelf.checkPermission({
    appName: 'hzzTest',
    type: 'address', // if you did not set type, it aways get by domain.
    address: '4WBgSL2fSem9ABD4LLZBpwP8eEymVSS1AyTBCqXjt5cfxXK'
}, (error, result) => {
    console.log('checkPermission>>>>>>>>>>>>>>>>>', result);
});

// result = {
//     ...,
//     permissions:[
//         {
//             address: '...',
//             appName: 'hzzTest',
//             contracts: [{
//                 chainId: 'AELF',
//                 contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
//                 contractName: 'token',
//                 description: 'token contract',
//                 github: ''
//             },
//             {
//                 chainId: 'AELF TEST',
//                 contractAddress: 'TEST contractAddress',
//                 contractName: 'TEST contractName',
//                 description: 'contract description',
//                 github: ''
//             }],
//             domian: 'Dapp domain'
//         }
//     ]
// }
```

<span id="get-address"></span>

### 7.GET_ADDRESS

```javascript
aelf.getAddress({
    appName: 'hzzTest'
}, (error, result) => {
    console.log('>>>>>>>>>>>>>>>>>>>', result);
});

// result = {
//     ...,
//     addressList: [
//         {
//             address: '...',
//             name: '...',
//             publicKey: {
//                 x: '...',
//                 y: '...'
//             }
//         }
//     ]
// }

```

<span id="set-contract-permission"></span>

### 8.SET_CONTRACT_PERMISSION

```javascript
aelf.setContractPermission({
    appName: 'hzzTest',
    hainId: 'AELF',
    payload: {
        address: '2JqnxvDiMNzbSgme2oxpqUFpUYfMjTpNBGCLP2CsWjpbHdu',
        contracts: [{
            chainId: 'AELF',
            contractAddress: 'TEST contractAddress',
            contractName: 'AAAA',
            description: 'contract description',
            github: ''
        }]
    }
}, (error, result) => {
    console.log('>>>>>>>>>>>>>', result);
});

// keychain = {
//     keypairs: {...},
//     permissions: [{
//         appName: 'hzzTest',
//         address: 'your keyparis address',
//         contracts: [{
//             chainId: 'AELF',
//             contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
//             contractName: 'token',
//             description: 'token contract',
//             github: '',
//             whitelist: {}
//         },
//         {
//             chainId: 'AELF',
//             contractAddress: 'TEST contractAddress',
//             contractName: 'AAAA',
//             description: 'contract description',
//             github: ''
//         }],
//         domain: 'Dapp domain'
//     }]
// }

```

<span id="remove-contract-permission"></span>

### 9.REMOVE_CONTRACT_PERMISSION

```javascript
aelf.removeContractPermission({
    appName: 'hzzTest',
    chainId: 'AELF',
    payload: {
        contractAddress: '2Xg2HKh8vusnFMQsHCXW1q3vys5JxG5ZnjiGwNDLrrpb9Mb'
    }
}, (error, result) => {
    console.log('removeContractPermission>>>>>>>>>>>>>>>>>>>', result);
});

// keychain = {
//     keypairs: {...},
//     permissions: [{
//         appName: 'hzzTest',
//         address: 'your keyparis address',
//         contracts: [{
//             chainId: 'AELF',
//             contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
//             contractName: 'token',
//             description: 'token contract',
//             github: ''
//         }],
//         domain: 'Dapp domain'
//     }]
// }

```

<span id="remove-methods-whitelist"></span>

### 10.REMOVE_METHODS_WHITELIST

```javascript
aelf.removeMethodsWhitelist({
    appName: 'hzzTest',
    chainId: 'AELF',
    payload: {
        contractAddress: '2Xg2HKh8vusnFMQsHCXW1q3vys5JxG5ZnjiGwNDLrrpb9Mb',
        whitelist: ['Approve']
    }
}, (error, result) => {
    console.log('removeWhitelist>>>>>>>>>>>>>>>>>', result);
});
// keychain = {
//     keypairs: {...},
//     permissions: [{
//         appName: 'hzzTest',
//         address: 'your keyparis address',
//         contracts: [{
//             chainId: 'AELF',
//             contractAddress: '4rkKQpsRFt1nU6weAHuJ6CfQDqo6dxruU3K3wNUFr6ZwZYc',
//             contractName: 'token',
//             description: 'token contract',
//             github: '',
//             whitelist: {}
//         }],
//         domain: 'Dapp domain'
//     }]
// }
```

## For Extension Developers

1. Download the code

```shell
   git clone https://github.com/hzz780/aelf-web-extension.git
```

2. Install dependent

```shell
    npm install
```

3. Run webpack

```shell
    webpack -w
```

4. Add to the browser

```shell
    open development mode, add the webpack output app/public.
```

## Project Information

We use [ECDH](https://github.com/indutny/elliptic) to use public key to  encryt data and private key to decrypt data.