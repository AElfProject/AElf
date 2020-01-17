# Transaction execution context

This article will present some of the functionality available to smart contract developers to help them implement common scenarios and more.

When executing, transactions trigger the logic contained inside smart contracts. The smart contract execution is mostly sandboxed (it’s an isolated environment) but some elements are accessible to the smart contract author through the **execution context**. 

Before we get started with the examples, It’s important to know a little about the execution model of transactions, this will help understand some concepts explained in this article. As a reminder this is what a transaction in AElf looks like (simplified):

```protobuf
message Transaction {
    Address from; // the address of the signer
    Address to;     // the address of the target contract 
    string method_name; // the method to execute
    bytes params;    // the parameters to pass to the method 
    bytes signature; // the signature of this transaction (by the Sender)
}
```

When users create and send a transaction to a node, it will eventually be packaged in a block. When this block is executed, the transactions are executed one by one. 

Each transaction can generate new transactions called inline transactions (more on this in the next article). When this happens the inline transactions generated are executed right after the transaction that generated them. For example, when executing a block with 2 transactions: TX1 and TX2 and the method executed by TX1 performs 2 inline calls. In this situation, the order of execution will be:
1. execute TX1
2.    - Execute first inline 
3.    - Execute second Inline
4. execute TX2

This is important to know because as we will see next some of the execution contexts values change based on this logic.

## Origin, Sender and Self

- **Origin**: the address of the sender (signer) of the transaction being executed. It’s type is an AElf address. It corresponds to the **From** field of the transaction. This value never changes, even for nested inline calls. This means that when you access this property in your contract, it’s value will be the entity that created the transaction (user or smart contract through an inline call) 
- **Self**: the address of the contract currently being executed. This changes for every transaction and inline transaction.
- **Sender**: the address sending the transaction. If the transaction execution does not produce any inline transactions this will always be the same. But if one contract calls another with an inline transaction, the sender will be the contract that is calling.

To use these values you can simply access them through the **Context** property.

```protobuf
Context.Origin
Context.Sender
Context.Self
```
