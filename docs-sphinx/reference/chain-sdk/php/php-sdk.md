# aelf-sdk.php - AELF PHP API

## Introduction

aelf-sdk.php for aelf is like web.js for ethereum.

aelf-sdk.php is a collection of libraries which allow you to interact with a local or remote aelf node, using a HTTP connection.

The following documentation will guide you through installing and running aelf-sdk.php, as well as providing a API reference documentation with examples.

If you need more information you can check out the repo : [aelf-sdk.php](https://github.com/AElfProject/aelf-sdk.php))

## Adding AElf php SDK

In order to install this library via composer run the following command in the console:

```bash
$ composer require aelf/aelf-sdk dev-dev
```
composer require curl/curl

If you directly clone the sdk You must install composer and execute it in the root directory 

```bash
"aelf/aelf-sdk": "dev-dev"
```

## Examples

You can also see full examples in `./test`;

### 1.Create instance

Create a new instance of AElf, connect to an AELF chain node. Using this instance, you can call the APIs on AElf.

```php
require_once 'vendor/autoload.php';
use AElf\AElf;
$url = '127.0.0.1:8000';
$aelf = new AElf($url);
```

### 2.Get a system contract address

Get a system contract address, take `AElf.ContractNames.Token` as an example

```php
require_once 'vendor/autoload.php';
use AElf\AElf;
$url = '127.0.0.1:8000';
$aelf = new AElf($url);

$privateKey = 'cd86ab6347d8e52bbbe8532141fc59ce596268143a308d1d40fedf385528b458';
$bytes = new Hash();
$bytes->setValue(hex2bin(hash('sha256', 'AElf.ContractNames.Token')));
$contractAddress = $aelf->GetContractAddressByName($privateKey, $bytes);
```

### 3.Send a transaction

Get the contract address, and then send the transaction.

```php
require_once 'vendor/autoload.php';
use AElf\AElf;
$url = '127.0.0.1:8000';
// create a new instance of AElf
$aelf = new AElf($url);

// private key
$privateKey = 'cd86ab6347d8e52bbbe8532141fc59ce596268143a308d1d40fedf385528b458';

$aelfEcdsa = new BitcoinECDSA();
$aelfEcdsa->setPrivateKey($privateKey);
$publicKey = $aelfEcdsa->getUncompressedPubKey();
$address = $aelfEcdsa->hash256(hex2bin($publicKey));
$address = $address . substr($aelfEcdsa->hash256(hex2bin($address)), 0, 8);
// sender address
$base58Address = $aelfEcdsa->base58_encode($address);

// transaction input
$params = new Hash();
$params->setValue(hex2bin(hash('sha256', 'AElf.ContractNames.Vote')));

// transaction method name
$methodName = "GetContractAddressByName";

// transaction contract address
$toAddress = $aelf->getGenesisContractAddress();

// generate a transaction
$transactionObj = aelf->generateTransaction($base58Address, $toAddress, $methodName, $params);

//signature
$signature = $aelf->signTransaction($privateKey, $transactionObj);
$transactionObj->setSignature(hex2bin($signature));

// obj Dto
$executeTransactionDtoObj = ['RawTransaction' => bin2hex($transaction->serializeToString())];

$result = $aelf->sendTransaction($executeTransactionDtoObj);
print_r($result);
```

## Web API

*You can see how the Web Api of the node works in `{chainAddress}/swagger/index.html`*
_tip: for an example, my local address: 'http://127.0.0.1:1235/swagger/index.html'_

The usage of these methods is based on the AElf instance, so if you don't have one please create it:

```php
require_once 'vendor/autoload.php';
use AElf\AElf;
$url = '127.0.0.1:8000';
// create a new instance of AElf
$aelf = new AElf($url);
```

### 1.getChainStatus

Get the current status of the block chain.

_Web API path_

`/api/blockChain/chainStatus`

_Parameters_

Empty

_Returns_

`Array`

- `ChainId - String`
- `Branches - Array`
- `NotLinkedBlocks - Array`
- `LongestChainHeight - Integer`
- `LongestChainHash - String`
- `GenesisBlockHash - String`
- `GenesisContractAddress - String`
- `LastIrreversibleBlockHash - String`
- `LastIrreversibleBlockHeight - Integer`
- `BestChainHash - String`
- `BestChainHeight - Integer`


_Example_

```php
// create a new instance of AElf
$aelf = new AElf($url);

$chainStatus = $aelf->getChainStatus();
print_r($chainStatus);
```

### 2.getBlockHeight

Get current best height of the chain.

_Web API path_

`/api/blockChain/blockHeight`

_Parameters_

Empty

_Returns_

`Integer`

_Example_
```php
$aelf = new AElf($url);

$height = $aelfClient->GetBlockHeight();
print($height);
```

### 3.getBlock

Get block information by block hash.

_Web API path_

`/api/blockChain/block`

_Parameters_

1. `block_hash - String`
2. `include_transactions - Boolean` :
  - `true` require transaction ids list in the block
  - `false` Doesn't require transaction ids list in the block

_Returns_

`Array`

- `BlockHash - String`
- `Header - Array`
  - `PreviousBlockHash - String`
  - `MerkleTreeRootOfTransactions - String`
  - `MerkleTreeRootOfWorldState - String`
  - `Extra - List`
  - `Height - Integer`
  - `Time - String`
  - `ChainId - String`
  - `Bloom - String`
  - `SignerPubkey - String`
- `Body - Array`
  - `TransactionsCount - Integer`
  - `Transactions - Array`
    - `transactionId - String`

_Example_
```php
$aelf = new AElf($url);

$block = $aelf->getBlockByHeight(1, true);
$block2 = $aelf->getBlockByHash($block['BlockHash'], false);
print_r($block2);
```

### 4.getBlockByHeight

_Web API path_

`/api/blockChain/blockByHeight`

Get block information by block height.

_Parameters_

1. `block_height - Number`
2. `include_transactions - Boolean` :
  - `true` require transaction ids list in the block
  - `false` Doesn't require transaction ids list in the block

_Returns_

`Array`

- `BlockHash - String`
- `Header - Array`
  - `PreviousBlockHash - String`
  - `MerkleTreeRootOfTransactions - String`
  - `MerkleTreeRootOfWorldState - String`
  - `Extra - List`
  - `Height - Integer`
  - `Time - String`
  - `ChainId - String`
  - `Bloom - String`
  - `SignerPubkey - String`
- `Body - Array`
  - `TransactionsCount - Integer`
  - `Transactions - Array`
    - `transactionId - String`

_Example_
```php
$aelf = new AElf($url);

$block = $aelf->getBlockByHeight(1, true);
print_r($block);
```

### 5.getTransactionResult

Get the result of a transaction

_Web API path_

`/api/blockChain/transactionResult`

_Parameters_

1. `transactionId - String`

_Returns_

`Object`

- `TransactionId - String`
- `Status - String`
- `Logs - Array`
  - `Address - String`
  - `Name - String`
  - `Indexed - Array`
  - `NonIndexed - String`
- `Bloom - String`
- `BlockNumber - Integer`
- `Transaction - Array`
  - `From - String`
  - `To - String`
  - `RefBlockNumber - Integer`
  - `RefBlockPrefix - String`
  - `MethodName - String`
  - `Params - json`
  - `Signature - String`
- `ReadableReturnValue - String`
- `Error - String`

_Example_
```php
$aelf = new AElf($url);

$block = $aelf->getBlockByHeight(1, true);
$transactionResult = $aelf->getTransactionResult($block['Body']['Transactions'][0]);
print_r('# get_transaction_result');
print_r($transactionResult);
```

### 6.getTransactionResults

Get multiple transaction results in a block

_Web API path_

`/api/blockChain/transactionResults`

_Parameters_

1. `blockHash - String`
2. `offset - Number`
3. `limit - Number`

_Returns_

  `List` - The array of method descriptions:
  - the transaction result object

_Example_
```php
$aelf = new AElf($url);

$block = $aelf->getBlockByHeight(1, true);
$transactionResults = $aelf->getTransactionResults($block['Body']);
print_r($transactionResults);
```

### 7.getTransactionPoolStatus

Get the transaction pool status.

_Web API path_

`/api/blockChain/transactionPoolStatus`

_Example_
```php
$aelf = new AElf($url);

$status = $aelf->getTransactionPoolStatus();
print_r($status);
```

### 8.sendTransaction

Broadcast a transaction

_Web API path_

`/api/blockChain/sendTransaction`

_POST_

_Parameters_

`transaction - String` - Serialization of data into String

_Example_
```php
$aelf = new AElf($url);

$params = new Hash();
$params->setValue(hex2bin(hash('sha256', 'AElf.ContractNames.Vote')));
$transaction = buildTransaction($aelf->getGenesisContractAddress(), 'GetContractAddressByName', $params);
$executeTransactionDtoObj = ['RawTransaction' => bin2hex($transaction->serializeToString())];
$result = $aelf->sendTransaction($executeTransactionDtoObj);
print_r($result);
```

### 9.sendTransactions

Broadcast multiple transactions

_Web API path_

`/api/blockChain/sendTransaction`

_POST_

_Parameters_

`transactions - String` - Serialization of data into String

_Example_
```php
$aelf = new AElf($url);

$paramsList = [$params1, $params2];
$rawTransactionsList = [];
foreach ($paramsList as $param) {
    $transactionObj = buildTransaction($toAddress, $methodName, $param);
    $rawTransactions = bin2hex($transactionObj->serializeToString());
    array_push($rawTransactionsList, $rawTransactions);
}
$sendTransactionsInputs = ['RawTransactions' => implode(',', $rawTransactionsList)];
$listString = $this->aelf->sendTransactions($sendTransactionsInputs);
print_r($listString);
```

### 10.getPeers

Get peer info about the connected network nodes

_Web API path_

`/api/net/peers`

_Example_
```php
$aelf = new AElf($url);

print_r($aelf->getPeers(true));
```

### 11.addPeer

Attempts to add a node to the connected network nodes

_Web API path_

`/api/net/peer`

_POST_

_Parameters_

`peer_address - String` - peer's endpoint

_Example_
```php
$aelf = new AElf($url);

$aelf->addPeer($url);
```

### 12.removePeer

Attempts to remove a node from the connected network nodes

_Web API path_

`/api/net/peer?address=`

_POST_

_Parameters_

`peer_address - String` - peer's endpoint

_Example_
```php
$aelf = new AElf($url);

$aelf->removePeer($url);
```

### 13.createRawTransaction

create a raw transaction

_Web API path_

`/api/blockchain/rawTransaction`

_POST_

_Parameters_

1. `transaction - Array`

_Returns_

`Array`

- `RawTransaction - hex string bytes generated by transaction information`

_Example_
```php
$aelf = new AElf($url);

$status = $aelf->getChainStatus();
$params = base64_encode(hex2bin(hash('sha256', 'AElf.ContractNames.Consensus')));
$param = array('value' => $params);
$transaction = [
    "from" => $aelf->getAddressFromPrivateKey($privateKey),
    "to" => $aelf->getGenesisContractAddress(),
    "refBlockNumber" => $status['BestChainHeight'],
    "refBlockHash" => $status['BestChainHash'],
    "methodName" => "GetContractAddressByName",
    "params" => json_encode($param)
];
$rawTransaction = $aelf->createRawTransaction($transaction);
print_r($rawTransaction);
```

### 14.sendRawTransaction

send raw transactions

_Web API path_

`/api/blockchain/sendRawTransaction`

_Parameters_

1. `Transaction - raw transaction`
2. `Signature - signature`
3. `ReturnTransaction - indicates whether to return transaction`

_Example_
```php
$aelf = new AElf($url);

$rawTransaction = $aelf->createRawTransaction($transaction);
$transactionId = hash('sha256', hex2bin($rawTransaction['RawTransaction']));
$sign = $aelf->getSignatureWithPrivateKey($privateKey, $transactionId);
$transaction = array('Transaction' => $rawTransaction['RawTransaction'], 'signature' => $sign, 'returnTransaction' => true);
$execute = $aelf->sendRawTransaction($transaction);
print_r($execute);
```

### 15.executeRawTransaction

execute raw transactions

_Web API path_

`/api/blockchain/executeRawTransaction`

_Post_

_Parameters_

1. `RawTransaction - raw transaction`
2. `Signature - signature`

_Example_
```php
$aelf = new AElf($url);

$rawTransaction = $aelf->createRawTransaction($transaction);
$transactionId = hash('sha256', hex2bin($rawTransaction['RawTransaction']));
$sign = $aelf->getSignatureWithPrivateKey($privateKey, $transactionId);
$transaction = array('RawTransaction' => $rawTransaction['RawTransaction'], 'signature' => $sign);
$execute = $aelf->executeRawTransaction($transaction);
print_r($execute);
```

### 16.getMerklePathByTransactionId

get merkle path

_Web API path_

`/api/blockchain/merklePathByTransactionId?transactionId=`

_Parameters_

1. `transactionId - String`

_Example_
```php
$aelf = new AElf($url);

$block = $aelf->getBlockByHeight(1, true);
$merklePath = $aelf->getMerklePathByTransactionId($block['Body']['Transactions'][0]);
```

### 17.getNetworkInfo

get network information

_Web API path_

`/api/net/networkInfo`

_Example_
```php
$aelf = new AElf($url);

print_r($aelf->getNetworkInfo());
```

### 18.getContractFileDescriptorSet

get contract file descriptor set

_Web API path_

`/api/blockChain/contractFileDescriptorSet`

_Example_
```php
$aelf = new AElf($url);

$blockDto = $aelf->getBlockByHeight($blockHeight, false);
$transactionResultDtoList = $aelf->getTransactionResults($blockDto['BlockHash'], 0, 10);
foreach ($transactionResultDtoList as $v) {
  $request = $aelf->getContractFileDescriptorSet($v['Transaction']['To']);
  print_r($request);
}
```

### 19.getTaskQueueStatus

get task queue status

_Web API path_

`/api/blockChain/taskQueueStatus`

_Example_
```php
$aelf = new AElf($url);

$taskQueueStatus = $aelf->getTaskQueueStatus();
print_r($taskQueueStatus);
```

### 20.executeTransaction

execute transaction

_Web API path_

_Post_

`/api/blockChain/executeTransaction`

_Example_
```php
$aelf = new AElf($url);

$methodName = "GetNativeTokenInfo";
$bytes = new Hash();
$bytes->setValue(hex2bin(hash('sha256', 'AElf.ContractNames.Token')));
$toAddress = $aelf->GetContractAddressByName($privateKey, $bytes);
$param = new Hash();
$param->setValue('');
$transaction = $aelf->generateTransaction($fromAddress, $toAddress, $methodName, $param);
$signature = $aelf->signTransaction($privateKey, $transaction);
$transaction->setSignature(hex2bin($signature));
$executeTransactionDtoObj = ['RawTransaction' => bin2hex($transaction->serializeToString())];
$response = $aelf->executeTransaction($executeTransactionDtoObj);
$tokenInfo = new TokenInfo();
$tokenInfo->mergeFromString(hex2bin($response));
```

## Other Tool Kit

AElf supply some APIs to simplify developing.

### 1.getChainId

get chain id

``` php
$aelf = new AElf($url);

$chainId = $aelf->getChainId();
print_r($chainId);
```

### 2.generateTransaction

generate a transaction object

``` php
$aelf = new AElf($url);

$param = new Hash();
$param->setValue('');
$transaction = $aelf->generateTransaction($fromAddress, $toAddress, $methodName, $param);
```

### 3.signTransaction

sign a transaction

``` php
$aelf = new AElf($url);

$transaction = $aelf->generateTransaction($fromAddress, $toAddress, $methodName, $param);
$signature = $aelf->signTransaction($privateKey, $transaction);
```

### 4.getGenesisContractAddress

get the genesis contract's address

``` php
$aelf = new AElf($url);

$genesisContractAddress = $aelf->getGenesisContractAddress();
print_r($genesisContractAddress);
```

### 4.getAddressFromPubKey

calculate the account address accoriding to the public key

``` php
$aelf = new AElf($url);

$pubKeyAddress = $aelf->getAddressFromPubKey('04166cf4be901dee1c21f3d97b9e4818f229bec72a5ecd56b5c4d6ce7abfc3c87e25c36fd279db721acf4258fb489b4a4406e6e6e467935d06990be9d134e5741c');
print_r($pubKeyAddress);
```

### 5.getFormattedAddress

convert the Address to the displayed string：symbol_base58-string_base58-string-chain-id.

``` php
$aelf = new AElf($url);

$addressVal = $aelf->getFormattedAddress($privateKey, $base58Address);
print_r($addressVal);
```

### 6.generateKeyPairInfo

generate a new key pair using ECDSA

``` php
$aelf = new AElf($url);

$pairInfo = $aelf->generateKeyPairInfo();
print_r($pairInfo);
```

### 7.getContractAddressByName

get contract's address from its name

``` php
$aelf = new AElf($url);

$bytes = new Hash();
$bytes->setValue(hex2bin(hash('sha256', 'AElf.ContractNames.Token')));
$contractAddress = $aelf->GetContractAddressByName($privateKey, $bytes);
print_r($contractAddress);
```

### 8.getAddressFromPrivateKey

get address from a private key

``` php
$aelf = new AElf($url);

$address = $aelf->getAddressFromPrivateKey($privateKey);
print_r($address);
```

### 9.getSignatureWithPrivateKey

given a private key, get the signature

``` php
$aelf = new AElf($url);

$sign = $aelf->getSignatureWithPrivateKey($privateKey, $transactionId);
print_r($sign);
```

### 10.isConnected

check if it connects the chain

``` php
$aelf = new AElf($url);

$isConnected = $this->aelf->isConnected();
print_r($isConnected);
```

### 11.getTransactionFees

get the transaction fee from transaction result

``` php
$aelf = new AElf($url);

$block = $aelf->getBlockByHeight(1, true);
$transactionResult = $aelf->getTransactionResult($block['Body']['Transactions'][0]);
$transactionFees = $aelf->getTransactionFees($transactionResult);
print_r($transactionFees);
```

## AElf.version

```php
$aelf = new AElf($url);

$version = $aelf->version;
```

## Requirements

- [php](https://www.php.org)

## About contributing

Read out [contributing guide]

## About Version

https://semver.org/
