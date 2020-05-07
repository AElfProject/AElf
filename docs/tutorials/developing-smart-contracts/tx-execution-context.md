# Transaction execution context

This article will present some of the functionality available to smart contract developers that can help them implement common scenarios.

When executing, transactions trigger the logic contained inside smart contracts. The smart contract execution is mostly sandboxed (it's an isolated environment), but some elements are accessible to the smart contract author through the **execution context**. 

Before we get started with the examples, it's important to know a little about the execution model of transactions; this will help you understand some concepts explained in this article. As a reminder this is what a transaction in AElf looks like (simplified):

```protobuf
message Transaction {
    Address from;       // the address of the signer
    Address to;         // the address of the target contract 
    string method_name; // the method to execute
    bytes params;       // the parameters to pass to the method 
    bytes signature;    // the signature of this transaction (by the Sender)
}
```

When users create and send a transaction to a node, it will eventually be packaged in a block. When this block is executed, the transactions it contains are executed one by one. 

Each transaction can generate new transactions called inline transactions (more on this in the next article). When this happens, the generated inline transactions are executed right after the transaction that generated them. For example, let's consider the following scenario: a block with two transactions, let's say **tx1** and **tx2**, where **tx1** performs two inline calls. In this situation, the order of execution will be the following:

`"
1. execute tx1 
2.    - Execute first inline 
3.    - Execute second Inline 
4. execute tx2 
`"

This is important to know because, as we will see next, some of the execution context's values change based on this logic.

## Origin, Sender and Self

- **Origin**: the address of the sender (signer) of the transaction being executed. Its type is an AElf address. It corresponds to the **From** field of the transaction. This value never changes, even for nested inline calls. This means that when you access this property in your contract, it's value will be the entity that created the transaction (user or smart contract through an inline call) 
- **Self**: the address of the contract currently being executed. This changes for every transaction and inline transaction.
- **Sender**: the address sending the transaction. If the transaction execution does not produce any inline transactions, this will always be the same. But if one contract calls another with an inline transaction, the sender will be the contract that is calling.

To use these values, you can access them through the **Context** property.

```protobuf
Context.Origin
Context.Sender
Context.Self
```

## Useful properties

There are other properties that can be accessed through the context:
- transaction ID: this is the id of the transaction that is currently being executed. Note that inline transactions have their own ID.
- chain ID: the ID of the current chain, this can be useful in the contract that needs to implement cross-chain scenarios.
- current height: the height of the block that contains the transaction currently executing.
- current block time: the time included in the header of the current block.
- previous block hash: the hash of the block that precedes the current.

## Useful methods

### Logging and events: 

Fire log event - these are logs that can be found in the transaction result after execution. 

```csharp
public override Empty Vote(VoteMinerInput input)
{
    // for example the election system contract will fire a 'voted' event 
    // when a user calls vote.
    Context.Fire(new Voted
    {
        VoteId = input.VoteId,
        VotingItemId = votingRecord.VotingItemId,
        Voter = votingRecord.Voter
        //...
    });
}
```

Application logging - when writing a contract, it is useful to be able to log some elements in the applications log file to simplify development. Note that these logs are only visible when the node executing the transaction is build in **debug** mode.

```csharp
private Hash AssertValidNewVotingItem(VotingRegisterInput input)
{
    // this is a method in the voting contract that will log to the applications log file
    // when a 'voting item' is created. 
    Context.LogDebug(() => "Voting item created by {0}: {1}", Context.Sender, votingItemId.ToHex());
    // ...
}
```

### Get contract address

It's sometimes useful to get the address of a system contract; this can be done as follows:

```csharp
    public override Empty AddBeneficiary(AddBeneficiaryInput input)
    {
        // In the profit contract, when adding a 'beneficiary', the method will get the address of the token holder 
        // contract from its name, to perform an assert.

        Assert(Context.Sender == Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName),
        "Only manager can add beneficiary.");
    }
```

### Recovering the public key

Recovering the public key: this can be used for recovering the public key of the transaction Sender.

```csharp
public override Empty Vote(VoteMinerInput input)
{
    // for example the election system contract will use the public key of the sender
    // to keep track of votes.
    var recoveredPublicKey = Context.RecoverPublicKey();
}
```

## Next

The execution context also exposes functionality for sending inline transactions; the next article will give you more details on how to generate inline calls.