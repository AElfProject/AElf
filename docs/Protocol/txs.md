## Transactions

Transactions utlimatly are what will change the state of the blockchain, by calling methods on contracts. A transaction is either sent to the node via RPC or received from the network. The following message describes the structure of a transaction:

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

this will build the transaction to the contract at address "65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9" that will call SomeMethod with encoded params. The params are serialized by the following function:

Next you will need to sign the transaction:
```js
import Aelf from 'aelf-sdk';
var txn = Aelf.wallet.signTransaction(rawTxn, wallet.keyPair);
```

And finally, serialize the
```js
tx = proto.Transaction.encode(tx).finish();
```

For the full process check out "ContractMethod.prototype.toPayload".