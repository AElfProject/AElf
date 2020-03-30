# aelf-bridge

JS library for dApps's communication with iOS Android or other applications implementing the protocol for aelf-bridge.

At the time of writing (2019.12), aelf [iOS wallet](https://github.com/AElfProject/aelf-wallet-ios), [Android wallet](https://github.com/AElfProject/aelf-wallet-android) had implemented this protocol.

[git repository: aelf-bridge](https://github.com/AElfProject/aelf-bridge)

[Protocol zh-CN](https://github.com/AElfProject/aelf-bridge/blob/master/PROTOCOL.zh-CN.md)

## 1. Why do we need this SDK

- DApps are not suppose to store any wallet information.
- The wallet application stores AElf wallet information and can communicate directly with the AElf chain.

In order to protect wallet information and provide dApps with the ability to interact with the chain,
aelf-bridge can be used for interacting with the wallet.

The wallet App described here includes a mobile (iOS/Android) native app, desktop app and more.

## 2. Adding aelf-bridge.js

First you need to get aelf-bridge.js into your project. This can be done using the following methods:

npm: `npm install aelf-bridge`

pure js: `link dist/aelf-bridge.js`

After that you need to create a aelf-bridge instance and connect.

```js
// <!-- use quickly in browser -->
// <script src="https://unpkg.com/aelf-bridge@latest/dist/aelf-bridge.js"></script>
// Use import
import AElfBridge from 'aelf-bridge';

// Initialize the bridge instance
// you can pass options during initialization to specify the behavior, see below for explanation
const bridgeInstance = new AElfBridge();
// init with options
const bridgeInstance = new AElfBridge({
  timeout: 5000 // ms
});

// After initializing the instance you need connect
bridgeInstance.connect().then(isConnected => {
  // isConnected True if the connection was successful.
})
```

## 3. How it works & DevTools

### 3.1.1 How it works

DApps are mostly web application which can communicate with clients in many ways.

`aelf-bridge` supports two of them:

- postMessage: a dApp will run in a container (`iframe` or mobile Apps' `webview`),
and the container needs to overwrite `window.postMessage` method in the dApp,
so the dApp and the container can communicate with each other by overwritten `postMessage`.  
- WebSocket(Socket.io): use traditional B/S architecture, communicate by `WebSocket`.
SDK uses `Socket.io` to support `WebSocket` communication, and this requires servers need to support `Socket.io` too.

### 3.1.2 Demo & DevTools for you

Developers can choose one of them depending on requirements, in the process of development,
we provide two ways to support data mock and debugging:

- [aelf-bridge-demo](https://github.com/AElfProject/aelf-bridge-demo):
this demo uses `iframe` to overwrite `dapp.html`'s `postMessage` to simulate communication with mobile App.
- [aelf-command dapp-server](https://github.com/AElfProject/aelf-command):
`aelf-command` provides a simple `socket.io` server to support the communication method `socket.io` in `aelf-bridge`,
developers can change the communication way to `SOCKET.IO`,
and give the URI given by running `aelf-command dapp-server` as an option when initializing `aelf-bridge` instance.
Therefore developers can inspect the communications in the Network tab of browser.

## 4. Usage

### 4.1 Options

The options can be passed as follows:

```javascript
// const bridgeInstance = new AElfBridge(defaultOptions);
const defaultOptions = {
  proxyType: String, // The default is `POST_MESSAGE`. Currently, we support the `POST_MESSAGE` and `SOCKET.IO` proxy types are provided. The `Websocket` mechanism will be provided in the future. Valid values ​​are available via `AElfBridge.getProxies()`.
  channelType: String, // The default is `SIGN`, it is the serialization of the request and response, that is, Dapp exchanges the public and private keys with the client and the private key is used to verify the signature information, thereby verifying whether the information has been tampered with. Another method of symmetric encryption is provided. The parameter value is `ENCRYPT`, and the shared public key is used for symmetric encryption. The valid value of the parameter is obtained by `AElfBridge.getChannels()`.
  timeout: Number, // Request timeout, defaults to 3000 milliseconds
  appId: String, // The default is empty. Dapp does not specify if there is no special requirement. If you need to specify it, you need to randomly generate a 32-bit hex-coded id each time. A credential used to communicate with the client, specifying the Dapp ID. If it is not specified, the library will process it internally. The first run will generate a random 32-bit hex-encoded uuid. After the connection is successful, it will be stored in `localStorage`, then the value will be taken from `localStorage`. If not, then Generate a random id.
  endpoint: String, // The default is empty. If the address of the node is empty, the client uses the internally saved primary link address by default, and can also specify to send a request to a specific node.
  // Optional options in `POST_MESSAGE` communication mode
  origin: String, // The default is `*`, the second parameter of the `postMessage` function, in most cases you do not need to specify
  checkoutTimeout: Number, // The default is `200`, in milliseconds, it checks the client's injected `postMessage`. In most cases, you don't need to specify this
  urlPrefix: String, // The default is `aelf://aelf.io?params=`, which is used to specify the protocol and prefix of the node. If the client does not have special requirements, it does not need to be changed.
  // Optional options in `socket.io` communication mode.
  socketUrl: String, // The address of the websocket connection, the default is `http://localhost:50845`
  socketPath: String, // Path to the connection address, the default is empty
  messageType: String // Pass the type of the socket.io message, the default is `bridge`
}
```

### 4.2 Get wallet account information

`bridgeInstance.account()`

```javascript
bridgeInstance.account().then(res => {
  console.log(res);
})
res = {
  "code": 0,
  "msg": "success",
  "errors": [],
  "data": {
    "accounts": [
      {
        "name": "test",
        "address": "XxajQQtYxnsgQp92oiSeENao9XkmqbEitDD8CJKfDctvAQmH6"
      }
     ],
    "chains": [
      {
        "url": "http://13.231.179.27:8000",
        "isMainChain": true,
        "chainId": "AELF"
      },
      {
        "url": "http://52.68.97.242:8000",
        "isMainChain": false,
        "chainId": "2112"
      },
      {
        "url": "http://52.196.227.200:8000",
        "isMainChain": false,
        "chainId": "2113"
      }
    ]
  }
}
```

### 4.3 Call contract method (read-only and send transaction)

* Send transaction `bridgeInstance.invoke(params)`
* Contract read-only method `bridgeInstance.invokeRead(params)`

The two parameters are similar:

`params`:

```javascript
argument = {
  name: String, // parameter name
  value: Boolean | String | Object | '...' // Parameter value, theoretically any Javascript type
}

params = {
  endpoint: String, // Optional. It can be used to specify the URL address of the chain node. If it is not filled, it defaults to the option when initializing the `AElfBridge` instance. If there is no initialization option, the wallet App defaults to its own stored primary node address.
  contractAddress: String, // Contract address
  contractMethod: String, // Contract method
  arguments: argument[] /// List of parameters for the contract methods, type is array, array type is the above `argument` type
}
```

Example:

* Call the `Transfer` method of the `Token` contract to initiate a transfer transaction

```javascript
bridgeInstance.invoke({
  contractAddress: 'mS8xMLs9SuWdNECkrfQPF8SuRXRuQzitpjzghi3en39C3SRvf',
  contractMethod: 'Transfer',
  arguments: [
      {
        name: "transfer",
        value: {
          amount: "10000000000",
          to: "fasatqawag",
          symbol: "ELF",
          memo: "transfer ELF"
        }
      }
    ]
}).then(console.log);
```

* Call the `GetNativeTokenInfo` method of the `Token` contract to get the native token information:

```javascript
bridge.invokeRead({
  contractAddress: 'mS8xMLs9SuWdNECkrfQPF8SuRXRuQzitpjzghi3en39C3SRvf', 
  contractMethod: 'GetNativeTokenInfo', 
  arguments: []
}).then(setResult).catch(setResult);
```

### 4.4 Calling the chain API

API for interacting with the node. The API available methods can be viewed by `{chain address}/swagger/index.html`, to get the currently supported APIs you can call `AElfBridge.getChainApis()`.

`bridgeInstance.api(params)`

The `params` parameters are as follows:

```javascript
argument = {
  name: String, // parameter name
  value: Boolean | String | Object | '...' // Parameter value, theoretically any Javascript type
}

params = {
  endpoint: String, // It is not required. It can be used to specify the URL address of the chain node. If it is empty, it defaults to the option given when initializing the `AElfBridge` instance. If there is no initialization option, the wallet App defaults to its own stored primary node address.
  apiPath: String, // Api path, valid values ​​get the supported values ​​via `AElfBridge.getChainApis()`
  arguments: argument[] // api parameter list
}
```

Example:

* Get block height

```javascript
bridgeInstance.api({
  apiPath: '/api/blockChain/blockHeight', // Api path
  arguments: []
}).then(console.log).catch(console.log)
```

### 4.5 disconnect

Used to disconnect from the client and clearing the public key information, etc.

`bridgeInstance.disconnect()`
