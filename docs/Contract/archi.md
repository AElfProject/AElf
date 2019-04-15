
Smart contracts, along with the blockchains data, form the heart of a blockchain system. They define through some predefined logic how and according to what rules the state of the blockchain is modified. 
A smart contract is a collection of methods that each act upon a certain set of state variables.

The logic contained in smart contracts is triggered by transactions. If a user of the blockchain wants to modify some state, he needs to build a transaction that will call a specific methods on some contract. When the transaction is included in a block and this block is executed, the modifications will be executed.

Smart contracts a part of what makes dApps possible. They implement part of the buisness layer: the part that gets included in the blockchain.

What follows in this section will give you a general overview of how AElf implements smart contracts. The other sections will walk you through different notions more specifically.

## Architecture overview

In AElf, Smart Contracts are defined like micro-services. This makes Smart Contracts independent of specific programming language. This implies for example that our Consensus Protocol essentially becomes a service, because it is defined through Smart Contract.

<p align="center">
  <img src="sc-as-service.png" width="300">
</p>

As showed in the diagram above, smart contracts functionality is defined within the kernel. The kernel defines the fundamental components and infrastructure associated with defining smart contracts as a service:
* SDK abstracts - high level entities that provide hook for smart contract services to interact with the chain.
* Execution - high level primitives defined for execution

### Chain interactions

Smart contract need to interact with the chain and have access to contexutal information. For this AElf defines a bridge and a bridge host. 

Non exhaustive list of the functionnalities and small description:
* transaction information
* methods
* call, send inline...
* events

#### State 

The main point of a smart contract is to read and/or modify state.

State provider ?

### Runtime and execution

When a blocks transactions are executed, every transaction will generate a trace.

- transaction trace (Kernel.SmartContractExecution)

### Sdk

AElf comes with a native C# SDK.

Any developer or company can develop an sdk and a runtime for a specific languages by creating an adapter to communicate with the bridge through gRPC.





