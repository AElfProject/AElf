# aelf-bridge

[Github](https://github.com/AElfProject/aelf-bridge)

## Introduction

为了给Dapp提供与链交互的能力，同时为了保护钱包信息，隔离Dapp与钱包信息，aelf-bridge可用于与钱包App之间的交互，钱包App保存有AElf的钱包信息，能够与AElf链直接交流。

In order to provide dApps with the ability to interact with the chain and to protect wallet information, aelf-bridge can be used for interacting with the wallet. The wallet application stores AElf wallet information and can communicate directly with the AElf chain.

此处描述的钱包App可能包括移动端(iOS/Android)原生App，桌面版应用等。

The wallet App described here includes a mobile (iOS/Android) native app and a desktop app.

## Installation

aelf-bridge是AElf生态的一环，由于Dapp大多为Web应用，因此提供`JavaScript`的版本，使用`Npm`作为版本管理工具。

aelf-bridge is part of the AElf ecosystem. Since dApps are mostly web applications, it provides a version for `JavaScript` and one using `Npm` as a version management tool.

### Using version management tools

```bash
npm i aelf-bridge
// or
yarn add aelf-bridge
```

### Using the script tag

```html
<script src="https://unpkg.com/aelf-bridge@lastest/dist/aelf-bridge.js"></script>
```

## Usage

### demo

[aelf-bridge-demo](https://github.com/AElfProject/aelf-bridge-demo)

### Initialization

```javascript
import AElfBridge from 'aelf-bridge';

// 初始化实例，初始化时可传入选项，用于指定行为，具体看下方解释
// Initialize the bridge instance, you can pass options during initialization to specify the behavior, see below for explanation
const bridgeInstance = new AElfBridge();
// init with options
const bridgeInstance = new AElfBridge({
  timeout: 5000 // ms
});

// 初始化实例后需要进行连接，与需要通信的端交换公钥，传递appId等
// After initializing the instance, you need to connect, exchange the public key with the terminal that needs to 
// communicate, and pass the appId, etc.
bridgeInstance.connect().then(isConnected => {
  // isConnected True if the connection was successful.
})
```

#### Options

The options can be passed as follows:

```javascript
const defaultOptions = {
  proxyType: String, // 默认为`POST_MESSAGE`，与客户端的通信方式，目前仅提供`POST_MESSAGE`和`SOCKET.IO`两种通信机制，未来还会提供`Websocket`机制。有效值可通过`AElfBridge.getProxies()`获取。
  // The default is `POST_MESSAGE`, which communicates with the client. Currently, only the communication mechanisms of `POST_MESSAGE` and `SOCKET.IO` are provided, and the `Websocket` mechanism will be provided in the future. Valid values ​​are available via `AElfBridge.getProxies()`.
  channelType: String, // 默认为`SIGN`，请求与响应的序列化方式，即Dapp与客户端互相交换公私钥，通过私钥签名，公钥验证签名信息，从而验证信息是否被篡改。另提供对称加密的方式，参数值为`ENCRYPT`，使用共享公钥进行对称加密。参数有效值通过`AElfBridge.getChannels()`获取。
  // The default is `SIGN`, the serialization of the request and response, that is, Dapp exchanges the public and private keys with the client, and the private key is used to verify the signature information, thereby verifying whether the information has been tampered with. Another method of symmetric encryption is provided. The parameter value is `ENCRYPT`, and the shared public key is used for symmetric encryption. The valid value of the parameter is obtained by `AElfBridge.getChannels()`.
  timeout: Number, // Request timeout, defaults to 3000 milliseconds
  appId: String, // 默认为空，Dapp无特殊需求的情况下不指定即可，如需指定，需要每次随机产生一个32位hex编码的id。用于与客户端通信的凭证，指定Dapp ID。未指定的情况下，本library内部会进行处理，首次运行产生一个随机的32位hex编码的uuid，连接成功后存入`localStorage`，之后则从`localStorage`中取值，如无，则再产生随机id。
  // The default is empty. Dapp does not specify if there is no special requirement. If you need to specify it, you need to randomly generate a 32-bit hex-coded id each time. A credential used to communicate with the client, specifying the Dapp ID. If it is not specified, the library will process it internally. The first run will generate a random 32-bit hex-encoded uuid. After the connection is successful, it will be stored in `localStorage`, then the value will be taken from `localStorage`. If not, then Generate a random id.
  endpoint: String, // 默认为空，链节点的地址，为空的情况下，客户端默认使用内部保存的主链地址，也可指定向特定的节点发送请求。
  // `POST_MESSAGE`通信方式下可选的选项
  // The default is empty. If the address of the link node is empty, the client uses the internally saved primary link address by default, and can also specify to send a request to a specific node.
  // Optional options in `POST_MESSAGE` communication mode
  origin: String, // 默认为`*`，`postMessage`函数的第二参数，绝大多数情况下不需要指定
  // The default is `*`, the second parameter of the `postMessage` function, in most cases you do not need to specify
  checkoutTimeout: Number, // 默认为`200`，单位毫秒，检查客户端注入的`postMessage`，绝大多数情况下不需要指定
  // The default is `200`, in milliseconds, check the client's injected `postMessage`. In most cases, you don't need to specify this
  urlPrefix: String, // 默认为`aelf://aelf.io?params=`，序列化后的信息需要通信的协议头，用于客户端做区分，如果客户端没有特殊改变的情况下，不需要改变
  // `socket.io`通信方式下可选的选项
  // The default is `aelf://aelf.io?params=`, the serialized information needs to communicate with the protocol header, //which is used by the client to make a distinction. If the client does not have special changes, it does not need to be changed.
  // Optional options in `socket.io` communication mode
  socketUrl: String, // The address of the websocket connection, the default is `http://localhost:50845`
  socketPath: String, // 连接地址的path，默认为空 / Path to the connection address, the default is empty
  messageType: String // 传递socket.io消息的type，默认为`bridge` / Pass the type of the socket.io message, the default is `bridge`
}
```

### Get wallet account information

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
    "accountName": "test",
    "address": "XxajQQtYxnsgQp92oiSeENao9XkmqbEitDD8CJKfDctvAQmH6",
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

### Call contract method (read-only and send transaction)

* Send transaction `bridgeInstance.invoke(params)`
* Contract read-only method `bridgeInstance.invokeRead(params)`

The two parameters are similar:

`params`:
```javascript
argument = {
  name: String, // parameter name
  value: Boolean | String | Object | '...' // 参数值，理论上可谓任意类型 / Parameter value, theoretically any type
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

### Calling the chain API

用于调用链节点的API. API列表可通过`{链地址}/swagger/index.html`查看，目前支持的API可以通过：
`AElfBridge.getChainApis()`获取支持的列表

API for interacting with the node. API list can be viewed by `{chain address}/swagger/index.html`, currently supported APIs can be passed: `AElfBridge.getChainApis()` Get a list of supported

`bridgeInstance.api(params)`

The `params` parameters are as follows:
```javascript
argument = {
  name: String, // parameter name
  value: Boolean | String | Object | '...' // 参数值，理论上可谓任意类型 / Parameter value, theoretically any type
}

params = {
  endpoint: String, // 非必填，可用于指定链节点的URL地址，不填的情况下默认为初始化`AElfBridge`实例时的的选项，如无初始化选项，则钱包App默认为自己存储的主链节点地址 / It is not required. It can be used to specify the URL address of the chain node. If it is not filled, it defaults to the option when initializing the `AElfBridge` instance. If there is no initialization option, the wallet App defaults to its own stored primary node address.
  apiPath: String, // api路径，有效值通过`AElfBridge.getChainApis()`获取支持的值 / Api path, valid values ​​get the supported values ​​via `AElfBridge.getChainApis()`
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

### disconnect

用于断开与端上的连接，清除交换的公钥信息等 / Used to disconnect from the end, clear the exchanged public key information, etc.

`bridgeInstance.disconnect()`
