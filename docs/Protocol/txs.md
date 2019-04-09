## Transactions

Transactions utlimatly are what will change the state of the blockchain, by calling methods on contracts. A transaction is either sent to the node via RPC or received from the network. When broadcasting a transaction and if valid it will be eventually included in a block. When this block is received and executed by the node, it will potential change the state of contracts. The following message describes the structure of a transaction:

```Proto
option csharp_namespace = "AElf.Kernel";
import "common.proto";

message Transaction {
    Address From = 1;
    Address To = 2;
    int64 RefBlockNumber = 3;
    bytes RefBlockPrefix = 4;
    uint64 IncrementId = 5;
    string MethodName = 6;
    bytes Params = 7;
    uint64 Fee = 8;
    repeated bytes Sigs = 9;
}
```

This is the protobuf definition we use to serialize Transactions. Some important fields:
    - To: it is the address of the contract when calling a contract.
    - RefBlockNumber/Prefix: this a security measure, it will be explained is the Advanced section.
    - MethodName is the name of a method in the smart contract at the **To** address.
    - Params: the parameters to pass to the method.
    - Sigs: the signatures of this transaction.

Note that the **From** is not currently useful because we derive it from the signature.

In the js sdk theres multiple methods to work with transactions. One important method is the **getTransaction** method that will build a transaction object for you:

```js
import Aelf from 'aelf-sdk';
var rawTxn = proto.getTransaction('65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9''65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9', 'SomeMethod', encodedParams);
```

This will build the transaction to the contract at address "65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9" that will call **SomeMethod** with encoded params. The params are serialized by the following function:

#### Signature 

When signing a transaction it's actually a subset of the fields: from/to and the target method as well as the parameter that were given. It also contains the reference block number and prefix. 

You can use the js-sdk to sign the transaction with the following method:
```js
import Aelf from 'aelf-sdk';
var txn = Aelf.wallet.signTransaction(rawTxn, wallet.keyPair);
```