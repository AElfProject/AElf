# How to develop a DAPP by JS SDK

Please see `Develop Smart Contract` at first.

You can get the [document](https://github.com/AElfProject/aelf-sdk.js) of JS SDK.

You can get the [demo](https://github.com/AElfProject/aelf-boilerplate/);

## Install JS SDK

```bash
npm install aelf-sdk
```

## Import SDK and Init Contract

```js
const AElf = require('aelf-sdk');
const Wallet = AElf.wallet;

const helloWorldContractAddress = '4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6';

// address: 65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9
const defaultPrivateKey = 'bdb3b39ef4cd18c2697a920eb6d9e8c3cf1a930570beb37d04fb52400092c42b';

const wallet = Wallet.getWalletByPrivateKey(defaultPrivateKey);
const aelf = new AElf(new AElf.providers.HttpProvider('http://127.0.0.1:1728/chain'));

const helloWorldC = aelf.chain.contractAt(helloWorldContractAddress, wallet);

// 1.Good Way; async
// use `call` to get information is always a good way.
helloWorldC.Hello.call((err, result) => {
    console.log('call: ', err, result);
});
// { Value: 'Hello world!' };

// 2.Bay Way; sync
const result = helloWorldC.Hello();
console.log('not call: ', result);
// return demo:
// {
//     TransactionId: 'd40654c3f95a8a1b163f6d8b9112b0b72273ba74d02d2233b0c869db3847e35a'
// }
aelf.chain.getTxResult(result.TransactionId, (err, result) => {
    console.log(err, result);
});
// {
//     ...
//     ReadableReturnValue: '{ "Value": "Hello world!" }',
//     ...
// }

```
