Developing smart contracts
==========================

AElf is part of a relatively new software type called the blockchain.
From a high-level perspective, a blockchain is a network of
interconnected nodes that process transactions in order to form blocks.
Transactions are usually broadcast to the network by sending them to a
node; this node verifies the transaction, and if it’s correct will
broadcast it to other nodes. The client that sent the transaction can be
of many types, including a browser, script or any client that can
connect and send HTTP requests to a node.

Internally blockchains keep a record of all the transactions ever
executed by the network, and these transactions are contained in
cryptographically linked blocks. AElf uses a DPoS consensus type in
which miners collect transactions and, according to a schedule, package
them into blocks that are broadcast to the network. These linked blocks
effectively constitute the blockchain (here, blockchain refers to the
data structure rather than the software). In AElf the transaction and
blocks are usually referred to as **chain data**.

Smart contracts are pieces of code that can be executed by transactions,
and that will usually modify their associated state. In other words, the
execution of transactions modifies the current values of the contracts
state. The set of all the state variables of all the contracts is
referred to as a **state data**.

Contracts in AElf
~~~~~~~~~~~~~~~~~

Conceptually, AElf smart contracts are entities composed of essentially
three things: **action** methods, **view** methods, and the contracts
**state**. Actions represent logic that modifies the state of the
contract, and views are used to fetch the current state of the contract
without modifying it. Theses two types of methods are executed when a
transaction is being processed by a node, usually when executing a block
or producing it.

In practice, an aelf contract is written in C# with some parts that are
generated from a **protobuf definition**. The protobuf is used to define
the contract’s methods and data types. By using a custom plugin, the
protobuf compiler generates the C# code that is later extended by the
contract author to implement logic.

Development
~~~~~~~~~~~

Currently, the primary language supported by an AElf node is C#. The
provided **C# SDK** contains all essential elements for writing smart
contracts, including communication with the execution context, access to
state and storage primitives.

Writing a contract boils down to creating a protobuf definition and a C#
project (referred to sometimes as a Class Library in the C# world) and
referencing the SDK. Only a small subset of the C# language is needed to
develop a contract.

This series of articles mainly uses AElf Boilerplate as a smart contract
development framework. It takes care of the build process for the
contract author and provides some well-defined location to place the
contract files. The first article will show you how to set up this
environment. After the setup, the next three articles will walk you
through creating, testing, and deploying a contract. Later articles will
focus on exposing more complex functionality.

.. toctree::
   :caption: Smart contract development
   
   Setup Boilerplate <setup>
   Execution Context <tx-execution-context>
   Inline Contract Calls <internal-contract-call>
