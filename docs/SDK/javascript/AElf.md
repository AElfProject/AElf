<a name="AElf"></a>

## AElf
**Kind**: global class  

* [AElf](#AElf)
    * [new AElf(provider)](#new_AElf_new)
    * _instance_
        * [.setProvider(provider)](#AElf+setProvider)
        * [.reset(keepIsSyncing)](#AElf+reset)
        * [.isConnected()](#AElf+isConnected) ⇒ <code>boolean</code>
    * _static_
        * [.wallet](#AElf.wallet)
        * [.pbjs](#AElf.pbjs)
        * [.pbUtils](#AElf.pbUtils)
        * [.version](#AElf.version)

<a name="new_AElf_new"></a>

### new AElf(provider)
AElf


| Param | Type | Description |
| --- | --- | --- |
| provider | <code>Object</code> | the instance of HttpProvider |

**Example**  
```js
const aelf = new AElf(new AElf.providers.HttpProvider('https://127.0.0.1:8000/chain'))
```
<a name="AElf+setProvider"></a>

### aelf.setProvider(provider)
change the provider of the instance of AElf

**Kind**: instance method of [<code>AElf</code>](#AElf)  

| Param | Type | Description |
| --- | --- | --- |
| provider | <code>Object</code> | the instance of HttpProvider |

**Example**  
```js
const aelf = new AElf(new AElf.providers.HttpProvider('https://127.0.0.1:8000/chain'));
aelf.setProvider(new AElf.providers.HttpProvider('https://127.0.0.1:8010/chain'))
```
<a name="AElf+reset"></a>

### aelf.reset(keepIsSyncing)
reset

**Kind**: instance method of [<code>AElf</code>](#AElf)  

| Param | Type | Description |
| --- | --- | --- |
| keepIsSyncing | <code>boolean</code> | true/false |

**Example**  
```js
// keepIsSyncing = true/false
aelf.reset(keepIsSyncing);
```
<a name="AElf+isConnected"></a>

### aelf.isConnected() ⇒ <code>boolean</code>
check the rpc node is work or not

**Kind**: instance method of [<code>AElf</code>](#AElf)  
**Returns**: <code>boolean</code> - true/false whether can connect to the rpc.  
**Example**  
```js
aelf.isConnected()
// return true / false
```
<a name="AElf.wallet"></a>

### AElf.wallet
wallet tool

**Kind**: static property of [<code>AElf</code>](#AElf)  
<a name="AElf.pbjs"></a>

### AElf.pbjs
protobufjs

**Kind**: static property of [<code>AElf</code>](#AElf)  
<a name="AElf.pbUtils"></a>

### AElf.pbUtils
some method about protobufjs of AElf

**Kind**: static property of [<code>AElf</code>](#AElf)  
<a name="AElf.version"></a>

### AElf.version
get the verion of the SDK

**Kind**: static property of [<code>AElf</code>](#AElf)  