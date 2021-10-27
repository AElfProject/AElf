# Overview

Transactions ultimately are what will change the state of the blockchain by calling methods on smart contracts. A transaction is either sent to the node via RPC or received from the network. When broadcasting a transaction and if valid it will be eventually included in a block. When this block is received and executed by the node, it will potential change the state of contracts.

## Smart Contract

In AElf blockchain, smart contracts contains a set of **state** definitions and a set of methods which aiming at modifing these **state**s. 

## Action & View

In AElf blockchain, there are two types of smart contract methods, actions and views. Action methods will actually modify the state of one contract if a related transaction has included in a block and executed successfully. View methods cannot modify the state of this contract in any case.

Developers can claim a action method in proto file like this:

```protobuf
rpc Vote (VoteInput) returns (google.protobuf.Empty) {
}
```

And claim a view method like this:

```protobuf
rpc GetVotingResult (GetVotingResultInput) returns (VotingResult) {
    option (aelf.is_view) = true;
}
```

## Transaction Instance

Here's the defination of the Transaction.

``` protobuf
option csharp_namespace = "AElf.Types";

message Transaction {
    Address from = 1;
    Address to = 2;
    int64 ref_block_number = 3;
    bytes ref_block_prefix = 4;
    string method_name = 5;
    bytes params = 6;
    bytes signature = 10000;
}
```

In the js sdk, there are multiple methods to work with transactions. One important method is the **getTransaction** method that will build a transaction object for you:

```js
import Aelf from 'aelf-sdk';
var rawTxn = proto.getTransaction('65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9''65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9', 'SomeMethod', encodedParams);
```

This will build the transaction to the contract at address "65dDNxzcd35jESiidFXN5JV8Z7pCwaFnepuYQToNefSgqk9" that will call **SomeMethod** with encoded params.

### From

The address of the sender of a transaction.

Note that the **From** is not currently useful because we derive it from the signature.

### To

The address of the contract when calling a contract.

### MethodName

The name of a method in the smart contract at the **To** address.

### Params

The parameters to pass to the aforementioned method.

### Signature

When signing a transaction it's actually a subset of the fields: from/to and the target method as well as the parameter that were given. It also contains the reference block number and prefix. 

You can use the js-sdk to sign the transaction with the following method:

```js
import Aelf from 'aelf-sdk';
var txn = Aelf.wallet.signTransaction(rawTxn, wallet.keyPair);
```

### RefBlockNumber & RefBlockPrefix

These two fields measure whether this transaction has expired. The transaction will be discarded if it is too old.

## Transaction Id

The unique identity of a transaction. Transaction Id consists of a cryptographic hash of the instance basic fields, excluding signature.

Note that the Transaction Id of transactions will be the same if the sender broadcasted several transactions with the same origin data, and then these transactions will be regarded as one transaction even though broadcasting several times.

### Verify

One transaction now is verified by the node before forwarding this transaction to other nodes. If the transaction execution is failed, the node won't forward this transaction nor package this transaction to the producing block.

We have several transaction validationi providers such as:

- BasicTransactionValidationProvider. To verify the transaction signature and size.

- TransactionExecutionValidationProvider. To pre-execute this transaction before forwarding this transaction or really packaging this transaction to new block.

- TransactionMethodValidationProvider. To prevent transaction which call view-only contract method from packaging to new block.

### Execution

In AElf, the transaction is executed via .net reflection mechanism.

Besides, we have some transaction execution plugins in AElf main net. The execution plugins contain pre-execution plugins and post-execution plugins.

- FeeChargePreExecutionPlugin. This plugin is for charging method fees from transaction sender.

- MethodCallingThresholdPreExecutionPlugin. This plugin is for checking the calling threshold of a specific contract or contract method.

- ResourceConsumptionPostExecutionPlugin. This plugin is for charging resource tokens from called contract after transaction execution (thus we can know how much resource tokens are cost during the execution.)

### TransactionResult

Data structure of TransactionResult:

```protobuf
message TransactionResourceInfo {
    repeated aelf.ScopedStatePath write_paths = 1;
    repeated aelf.ScopedStatePath read_paths = 2;
    ParallelType parallel_type = 3;
    aelf.Hash transaction_id = 4;
    aelf.Hash contract_hash = 5;
    bool is_nonparallel_contract_code = 6;
}
```