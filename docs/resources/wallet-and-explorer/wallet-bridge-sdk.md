# aelf-bridge

[Github](https://github.com/AElfProject/aelf-bridge)

中文 | [English](./README.md)

## 目录
  - [简介](#简介)
  - [安装](#安装)
    - [版本管理](#使用版本管理工具)
    - [使用script标签](#使用script标签)  
  - [使用](#使用)
    - [demo](#demo)
    - [初始化](#初始化)
      - [选项](#选项)
    - [获取钱包账户信息](#获取钱包账户信息)
    - [调用合约方法(只读与发送交易)](#调用合约方法(只读与发送交易))
    - [调用链API](#调用链API)
    - [disconnect-断开连接](#disconnect-断开连接)
      
## 简介

为了给Dapp提供与链交互的能力，同时为了保护钱包信息，隔离Dapp与钱包信息，aelf-bridge可用于与钱包App之间的交互，钱包App保存有AElf的钱包信息，能够与AElf链直接交流。

此处描述的钱包App可能包括移动端(iOS/Android)原生App，桌面版应用等。

## 安装

aelf-bridge是AElf生态的一环，由于Dapp大多为Web应用，因此提供`JavaScript`的版本，使用`Npm`作为版本管理工具。

### 使用版本管理工具

```bash
npm i aelf-bridge
// or
yarn add aelf-bridge
```

### 使用script标签

```html
<script src="https://unpkg.com/aelf-bridge@lastest/dist/aelf-bridge.js"></script>
```

## 使用

### demo

[aelf-bridge-demo](https://github.com/AElfProject/aelf-bridge-demo)

### 初始化

```javascript
import AElfBridge from 'aelf-bridge';

// 初始化实例，初始化时可传入选项，用于指定行为，具体看下方解释
const bridgeInstance = new AElfBridge();
// 可传入选项
const bridgeInstance = new AElfBridge({
  timeout: 5000 // ms, 毫秒
});

// 初始化实例后需要进行连接，与需要通信的端交换公钥，传递appId等
bridgeInstance.connect().then(isConnected => {
  // isConnected 为true时表示连接成功
})
```

#### 选项

可传入的选项列表如下

```javascript
const defaultOptions = {
  proxyType: String, // 默认为`POST_MESSAGE`，与客户端的通信方式，目前仅提供`POST_MESSAGE`和`SOCKET.IO`两种通信机制，未来还会提供`Websocket`机制。有效值可通过`AElfBridge.getProxies()`获取。
  channelType: String, // 默认为`SIGN`，请求与响应的序列化方式，即Dapp与客户端互相交换公私钥，通过私钥签名，公钥验证签名信息，从而验证信息是否被篡改。另提供对称加密的方式，参数值为`ENCRYPT`，使用共享公钥进行对称加密。参数有效值通过`AElfBridge.getChannels()`获取。
  timeout: Number, // 默认为`3000`，请求的超时时间，单位为毫秒，目前只支持一个全局的超时时间。
  appId: String, // 默认为空，Dapp无特殊需求的情况下不指定即可，如需指定，需要每次随机产生一个32位hex编码的id。用于与客户端通信的凭证，指定Dapp ID。未指定的情况下，本library内部会进行处理，首次运行产生一个随机的32位hex编码的uuid，连接成功后存入`localStorage`，之后则从`localStorage`中取值，如无，则再产生随机id。
  endpoint: String, // 默认为空，链节点的地址，为空的情况下，客户端默认使用内部保存的主链地址，也可指定向特定的节点发送请求。
  // `POST_MESSAGE`通信方式下可选的选项
  origin: String, // 默认为`*`，`postMessage`函数的第二参数，绝大多数情况下不需要指定
  checkoutTimeout: Number, // 默认为`200`，单位毫秒，检查客户端注入的`postMessage`，绝大多数情况下不需要指定
  urlPrefix: String, // 默认为`aelf://aelf.io?params=`，序列化后的信息需要通信的协议头，用于客户端做区分，如果客户端没有特殊改变的情况下，不需要改变
  // `socket.io`通信方式下可选的选项
  socketUrl: String, // websocket连接的地址，默认为`http://localhost:50845`
  socketPath: String, // 连接地址的path，默认为空
  messageType: String // 传递socket.io消息的type，默认为`bridge`
}
```

### 获取钱包账户信息

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

### 调用合约方法(只读与发送交易)

* 发送交易`bridgeInstance.invoke(params)`
* 合约只读方法`bridgeInstance.invokeRead(params)`

二者参数一致，均需要构造参数

`params`:
```javascript
argument = {
  name: String, // 参数名
  value: Boolean | String | Object | '...' // 参数值，理论上可谓任意类型 
}

params = {
  endpoint: String, // 非必填，可用于指定链节点的URL地址，不填的情况下默认为初始化`AElfBridge`实例时的的选项，如无初始化选项，则钱包App默认为自己存储的主链节点地址
  contractAddress: String, // 合约的地址
  contractMethod: String, // 合约的方法
  arguments: argument[] // 合约方法的参数列表，类型为数组，数组类型为上述的`argument`
}
```

示例：

* 调用`Token`合约的`Transfer`方法，发起一笔转账交易
```javascript
bridgeInstance.invoke({
  contractAddress: 'mS8xMLs9SuWdNECkrfQPF8SuRXRuQzitpjzghi3en39C3SRvf', // 合约地址
  contractMethod: 'Transfer', // 合约方法名
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

* 调用`Token`合约的`GetNativeTokenInfo`方法，获取native token信息
```javascript
bridge.invokeRead({
  contractAddress: 'mS8xMLs9SuWdNECkrfQPF8SuRXRuQzitpjzghi3en39C3SRvf', // 合约地址
  contractMethod: 'GetNativeTokenInfo', // 合约方法名
  arguments: []
}).then(setResult).catch(setResult);
```

### 调用链API

用于调用链节点的API. API列表可通过`{链地址}/swagger/index.html`查看，目前支持的API可以通过：
`AElfBridge.getChainApis()`获取支持的列表

`bridgeInstance.api(params)`

`params`参数如下：
```javascript
argument = {
  name: String, // 参数名
  value: Boolean | String | Object | '...' // 参数值，理论上可谓任意类型 
}

params = {
  endpoint: String, // 非必填，可用于指定链节点的URL地址，不填的情况下默认为初始化`AElfBridge`实例时的的选项，如无初始化选项，则钱包App默认为自己存储的主链节点地址
  apiPath: String, // api路径，有效值通过`AElfBridge.getChainApis()`获取支持的值
  arguments: argument[] // api的参数列表
}
```

示例：

* 获取区块高度
```javascript
bridgeInstance.api({
  apiPath: '/api/blockChain/blockHeight', // api路径
  arguments: []
}).then(console.log).catch(console.log)
```

### disconnect-断开连接

用于断开与端上的连接，清除交换的公钥信息等

`bridgeInstance.disconnect()`
