# Developing smart contracts

## Overview of smart contract:

AElf is part of a relatively new software type called blockchain. From a high level perspective a blockchain is a network of interconnected nodes that process transactions in order to form blocks. Transactions are usually broadcast to the network by sending them to a node; this node verifies the transaction and if it’s correct will broadcast it to other nodes. The client that sent the transaction can be of many types, including a browser, script or any client that can connect and send HTTP requests to a node. 

Internally blockchains keep a record of all the transactions ever executed by the network and these transactions are contained in cryptographically linked blocks. AElf uses a DPoS consensus type in which miners collect transactions and according to a schedule package them into blocks that are broadcast to the network. These linked blocks effectively constitute the blockchain (here, blockchain refers to the data structure rather than the software). In AElf the transaction and blocks are usually referred to as **chain data**.

Smart contracts are pieces of code that can be executed by transactions and that will usually modify their associated state. In other word the execution of transactions modifies the current state of the chain. The set of all the state variables of all the contracts is referred to a **state data**.

### Contracts in AElf

In AElf smart contract are a entities composed of essentially three things: **action** methods, **view** methods and the contracts **state**. Actions represent logic that modify the state of the contract and views are used to fetch the current state of the contract without modifying the state. Theses two types of methods are executed when a transaction is being processed by a node, usually when executing a block or producing it. 

In practice, an aelf contract is written in C# with some parts that are generated from a **protobuf service definition**. The protobuf is used to define the contracts methods and data types. Using a custom plugin the protobuf compiler generates the C# code that is later extended by the contract author to implement logic.

### Development

Currently the main language supported by an AElf node is C#. The provided **C# SDK** contains all basic types for writing smart contracts, including communication with the execution context, access to state and storage primitives.

Writing a contract is boils down to creating a protobuf definition and a C# project (referred to sometimes as a Class library in the C# world) and referencing the SDK. 

This series of articles mainly uses AElf Boilerplate that takes care of the build process for the contract author. The first article will show you how to set up this environment. After the setup, the next three articles will walk you through creating, testing and deploying a contract. Later articles will focus on exposing more complex functionality.