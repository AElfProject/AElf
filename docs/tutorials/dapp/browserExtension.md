# How to develop a DAPP by Browser Extension

`Use JS SDK` will help you.

You can get the [document](https://github.com/hzz780/aelf-web-extension) of the Extension.

You can get this [demo in github](https://github.com/AElfProject/aelf-boilerplate).

For developers, extension is wrap of JS SDK.

For user, manage your assets safely.

## Interaction Flow

- 1.Make sure the user get the Extension
- 2.Connect Chain
- 3.Initialize Contract
- 4.Call contract methods

## 1.Intall the Extension

You can get the [dev version](https://chrome.google.com/webstore/detail/aelf-explorer-extension-d/mlmlhipeonlflbcclinpbmcjdnpnmkpf) now.

You can get the [lastest version here](https://github.com/hzz780/aelf-web-extension).

## 2.Check the Extension

```js
let nightElfInstance = null;
class NightElfCheck {
    constructor() {
        const readyMessage = 'NightElf is ready';
        let resovleTemp = null;
        this.check = new Promise((resolve, reject) => {
            if (window.NightElf) {
                resolve(readyMessage);
            }
            setTimeout(() => {
                reject({
                    error: 200001,
                    message: 'timeout / can not find NightElf / please install the extension'
                });
            }, 1000);
            resovleTemp = resolve;
        });
        document.addEventListener('NightElf', result => {
            console.log('test.js check the status of extension named nightElf: ', result);
            resovleTemp(readyMessage);
        });
    }
    static getInstance() {
        if (!nightElfInstance) {
            nightElfInstance = new NightElfCheck();
            return nightElfInstance;
        }
        return nightElfInstance;
    }
}
const nightElfCheck = NightElfCheck.getInstance();
nightElfCheck.check.then(message => {
    // connectChain -> Login -> initContract -> call contract methods
});
```

## Connect the chain

```js
const aelf = new window.NightElf.AElf({
    // Enter your test address in this location
    httpProvider: [
        'http://127.0.0.1:1235/chain',
        null,
        null,
        null,
        [{
            name: 'Accept',
            value: 'text/plain;v=1.0'
        }]
    ],
    appName: 'appName'
});

aelf.chain.getChainStatus((error, result) => {
    console.log('>>>>>>>>>>>>> getChainStatus >>>>>>>>>>>>>');
    console.log(error, result);
});
```

## Login

If the user have not logged in the DAPP, there will be a prompt to let the user log in.

If the user had logged in th DAPP, you will get the address the user log in.

```js
aelf.login({
    appName,
    chainId: 'AELF',
    payload: {
        method: 'LOGIN',
        contracts: [{
            chainId: 'AELF',
            // In the demo: helloWorldAddress = 2UEEa5yiFhuh6JDfTGrbAFqoqzbKkY4Vk9YZDXAdw16wkMw
            contractAddress: helloWorldAddress,
            contractName: 'hello world',
            description: 'hello world contract',
            github: ''
        }]
    }
}, (err, result) => {
    console.log('>>>>>>> login >>>>>>>>>>>>', err, result);
    wallet = JSON.parse(result.detail);
});
```

## Init Contract && Call the contact method

```js
aelf.chain.contractAtAsync(
    helloWorldAddress,
    wallet,
    (error, result) => {
        console.log('>>>>>>>>>>>>> contractAtAsync >>>>>>>>>>>>>');
        console.log(error, result);
        const helloWorldC = result;
        helloWorldC.Hello.call((err, result) => {
            alert(result.Value);
        });
    }
);
```