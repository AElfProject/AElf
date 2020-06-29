# class CSharpSmartContract

This class represents a base class for contract's written in the C# language. The generated code from the protobuf definitions will inherit from this class.

## Properties

### StateProvider

```csharp
public CSharpSmartContractContext Context { get; private set; }
```

Represents the transaction execution context in a smart contract. It provides access inside the contract to properties and methods useful for implementing the smart contract's action logic. 

### State

```csharp
public TContractState State { get; internal set; }
```

Provides access to the State class instance. *TContractState* is the type of the state class defined by the contract author.