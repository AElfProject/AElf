# Technical documentation for the Ælf project

As the whitepaper already states the AElf kernel will be built in a similar way to the Linux kernel. It will implement the basic building blocks that will allow other developers to build “functionality” on top of it. The whitepaper also states that users can redefine the “core” through interface.

## Table of Contents

* [Data Structures](main-page.md#1data-structures)
* [Components](main-page.md#2components)
* [AElf Topology](main-page.md#3aelf-topology)

## Data Structures

### **Account**

Represents accounts in the system. The only field in account is a byte array \(through IHash\).

### **Block**

Composed of a Header and a Body. This structure is used as a container for transactions. Note that AElf blocks can reference an enormous amount of transactions \(1 millions for example\). It also contains the Merkle Tree \(similar to BTCs one\).

### **Transaction**

Represents a transaction in the AElf system. It is used to:

* Transfer tokens 
* Call a smart contract method
* Deploy a smart contract

### **SmartContractRegistration**

This class represents the registration of a smart contract. The Hash of SmartContractRegistration is calculated with contract name and the address of caller account who deploy the contract. And the hash is also the address of new account who is associated with this smart contract \(updated on 3.8\)

### **Chains**

Chains in the kernel represent accounts \(AccountManager\) and block data.

## **Serialization**

To learn more about how we organize serialization please check out [this page](serialization.md).

## Components

This section describes the components implemented in the kernel. It clarifies the roles that they have in the system.

## **Smart Contracts**

A `Smart Contract` can be seen as a protocol. It’s implemented as a service \(micro-service\). For example, this means that since the **Consensus Protocol** is defined as a `Smart Contract`, it is in fact a service. When a chain is created, it needs a genesis block with a collection of Smart Contracts deployed. This collection, named as **Genesis Smart Contract Collection**, can be changed in the future by vote. The contract code will be encapsulated in `SmartContractRegistration` and registered into `AccountZero`. `Smartcontract` objects should be cached in memory. `SmartContractZero` provides entries for `Smartcontract` using `SmartContractRegistration`.

### **Merkle Tree**

### **Provider**

* `AccountDataProvider` provides entries associated with given Account including:
  * `DataProvider` 
  * `AccountDataContext`
  * `Account Address` 
* `DataProvider` provides entries for data access related to given Account.

### **Service**

* `AccountContextService` provides functionality caching `AccountDataContext` objects in memory. 
  * Return `AccountDataContext` object in memory if cached
  * Return `AccountDataContext` object new created if not cached
* `ChainCreationService` provides functionality creating a new chain including building and appending `GenesisBlock`.
* `ChainContextService` provides functionality caching `ChainContext` objects in memory. 
  * Return `ChainContext` object in memory if cached
  * Return `ChainContext` object new created with `SmartContractZero` if not cached
* `BlockValidatingService` maintains `BlockValidationFilter` collection and provides entry of blocks validation.
* `SmartContractService` provides functionality for obtaining `SmartContract`

### **Manager**

* `BlockManager` provides entries\(get/set\) for `BlockStore`
* `ChainManager` provides functionality of appending the given Block to specified Chain and entries for `ChainBlockRelationStrore`
* `ChainManager` provides entries\(get/set）for `ChainStore`
* `SmartContractManager` provides entries\(get/set）for `SmartContractRegistration` storage
* `TransactionManager` provides entries for `TransactionSotre`
* `TransactionExecutingManager` contains scheduling algorithm and provides functionality of `Transaction` executing
* `WorldStateManager` provides entry for `WoldStateStore` and functionality to obtain `AccountDataProvider` objects associated with given `Account`

### **Storage**

| Storage | Description |
| :--- | :--- |
| `BlockStore` | Insert and get `BLock` |
| `ChainStore` | Insert, update and get `Chain` |
| `ChangesStore` | Insert and get a change of `path-pointer` |
| `PointerStore` | Insert and get `pointer` \(by path\) |
| `TransactionStore` | Insert and get `Transaction` |
| `WorldStateStore` | Insert and get `World State` of each `Block` |
| `ChainBlockRelationStore` | Insert and get `chain-block` relations by `Hash` |

### **Relation among Service, Manager, Storage**

* **Service** is processing logic associated with chain state.
* **Manager** provides functionalities having nothing to do with chain state.
* **Storage** provides storage access and persistence without logic.

```text
  +-------------+  +-------------+  +------------+     
  |             |  |             |  |            |     
  |  Service  +----->  Manager  +-----> Storage  |
  |             |  |             |  |            |
  +-------------+  +-------------+  +------------+
```

## AElf topology

![](../../.gitbook/assets/aelf-cluster-diagram%20%282%29.png)

