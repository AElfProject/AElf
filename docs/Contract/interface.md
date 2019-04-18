## Smart Contract Interface

In contrast to some other blockchain platforms, aelf has a few unique points regarding the smart contract interface:
1. smart contract interface is explicitly defined instead of extracted from the code
2. aelf uses protobuf to define the smart contract interface and hence supports multiple languages
3. code is generated from the interface declaration

Besides the plain keywords in protobuf, aelf defines custom options to denote the specific contract-related concepts on plain protobuf entities. In the following sections, let's go through the concepts.

### Service and Methods

A protobuf `service` is required for a contract. The `service` declares the methods that the contract is serving. The methods are specified with the `rpc` keyword. The inital usage of `service` and `rpc` methods are for declaring gRPC services. The way a smart contract works is similar to a gRPC service whereby the implemented methods handle requests for querying or updating some data. This makes `service` and `rpc` suitable for contract interface declaration. Please take note that only one `service` is allowed in a `.proto` file.

### View Methods

Each block in a blockchain logs all the transactions executed in that block. State changes and events resulted from transaction executions are also logged. However, sometimes users just want to query the current state. In this case, there will be no events or state changes and the transaction and result are not required to be logged. The contract developer can denotes a method is a query without effects by using the `aelf.is_view` option. A method with a `true` value for `aelf.is_view` option will be identified as a method that won't update the contract state.

### Events

An event is a data structure and has some fields to describe its details. In aelf, an event is declared as a message. To denote a message is an event, the `aelf.is_event` option has to be set to `true`. An event message type will be treated differently when the protobuf definition is used during code generation. aelf's smart contract platform provides a mechanism to fire an event during the execution of the transaction. The *fired* events will logged into the execution result. Together with the event log, a bloom filter is generated as an index for quickly find the interested in a block. If the value of a field is required to be indexed, the `aelf.is_indexed` field option has to be set to `true`.

### Inheritance

There are scenarios that the declarations of the methods may be from multiple sources. For example, a contract may be intended to implement a few standard contract interfaces (i.e. there are existing `proto` files containing the decalrations). To allow extension of the interface in a non-intrusive way, we create a `aelf.base` option for service. A `service` can declare multiple bases that it is extending. The value for the `aelf.base` should be the `.proto` file name for the inherited contract.

### Indentity

As described above, the `.proto` filename is used to identify a referenced contract declaration. It is fine within the context of a single contract. Unique name across multiple contracts cannot be enforced and is not necessary. However, aelf provides an `aelf.identity` file option for intended indentification of the contract declaration. A usecase for this is to allow system-wide special treatment of contracts implementing certain interface.