# How to develop a DAPP by JS SDK

Please see `Develop Smart Contract` at first.

You can get the [document](https://github.com/AElfProject/aelf-sdk.js) of JS SDK.

## Install JS SDK

```bash
npm install aelf-sdk
```

## Import SDK and Init Contract

```js
const AElf = require('aelf-sdk');
const Wallet = AElf.wallet;

// address: 65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9
const defaultPrivateKey = 'bdb3b39ef4cd18c2697a920eb6d9e8c3cf1a930570beb37d04fb52400092c42b';

const wallet = Wallet.getWalletByPrivateKey(defaultPrivateKey);
const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1728/chain'));

const helloWorldC = aelf.chain.contractAt('4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6', wallet);

// 1.Good Way
// use `call` to get information is always a good way.
helloWorldC.Hello.call();
// { Value: 'Hello world!' };

// 2.Bay Way
helloWorldC.Hello();
// return demo:
// {
//     TransactionId: 'd40654c3f95a8a1b163f6d8b9112b0b72273ba74d02d2233b0c869db3847e35a'
// }
aelf.chain.getTxResult('d40654c3f95a8a1b163f6d8b9112b0b72273ba74d02d2233b0c869db3847e35a');
// {
//     ...
//     ReadableReturnValue: '{ "Value": "Hello world!" }',
//     ...
// }
```