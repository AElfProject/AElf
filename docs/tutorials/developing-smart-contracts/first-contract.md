# Smart contract implementation

This article will guide you through how to use **AElf Boilerplate** to implement a smart contract. It takes an example on the **Greeter** contract that's already included in Boilerplate. Based on the concepts this article presents, you'll be able to create your own basic contract.

The previous article showed you how to **build, run, and test** a contract with the simple Hello World contract that is included in Boilerplate. This article is similar but more complete and will explain exactly how to add the elements of your contract and where to place them.

## Greeter contract

The following content will walk you through the basics of writing a smart contract; this process contains essentially four steps:
- **define the contract and its types**: the methods and types needed in your contract should be defined in a protobuf file, following typical protobuf syntax. 
- **create the project**: you can use other contracts in Boilerplate as a template (this tutorial explains some details more in-depth).
- **generate the code**: build the project to generate the base contract code from the proto definition.
- **extend the generated code**: implement the logic of the contract methods.

The ```Greeter``` contract is a very simple contract that exposes a ``Greet`` method that simply logs to the console and returns a "Hello World" message and a more sophisticated ```GreetTo``` method that records every greeting it receives and returns the greeting message as well as the time of the greeting.

{% hint style="info" %} 
This tutorial shows you how to develop a smart contract with the C# contract SDK; you can find you more [here](../../sdk/csharp/contract-sdk.md). Boilerplate will automatically add the reference to the SDK. 
{% endhint %} 

### Defining the contract

As stated above, the first step when writing a smart contract on AElf Boilerplate is to define the methods and types of your contract. AElf defines smart contracts as services that are implemented using gRPC and Protobuf. The definition contains no logic; at build time the proto file is used to generate C# classes that will be used to implement the logic and state of the contract.

We recommend putting the contract's definition in Boilerplate's **protobuf** folder so that it can easily be included in the build/generation process and also that you name the contract with the following syntax **contract_name_contract.proto**:

<!-- 
Boilerplate
## chain 
### protobuf
#### aelf
##### options.proto // contract options
##### core.proto    // core blockchain types
#### greeter_contract.proto
#### another_contract.proto
#### token_contract.proto // system contracts
#### acs0.proto // AElf contract standard
#### ...
-->

```
.
└── Boilerplate
    └── chain
        └── protobuf
            ├── aelf
            │   ├── options.proto // contract options
            │   └── core.proto    // core blockchain types
            ├── greeter_contract.proto
            ├── another_contract.proto
            ├── token_contract.proto // system contracts
            ├── acs0.proto // AElf contract standard
            └── ...
```

{% hint style="info" %} 
The "protobuf" folder already contains a certain amount of contract definitions, including tutorial examples, system contracts. You'll also notice it contains AElf Contract Standard definitions that are also defined the same way as contracts. Lastly, it also contains **options.proto** and **core.proto** that contain fundamental types for developing smart contracts, more on this later. 
{% endhint %} 

**Best practices:**
- place your contract definition in Boilerplate's **protobuf** folder.
- name your contract with **contractname_contract.proto**, all lower case.

Now let's take a look a the Greeter contract's definition:
``` Protobuf
// protobuf/greeter_contract.proto

syntax = "proto3";

import "aelf/options.proto";

import "google/protobuf/empty.proto";
import "google/protobuf/timestamp.proto";
import "google/protobuf/wrappers.proto";

option csharp_namespace = "AElf.Contracts.Greeter";

service GreeterContract { 
    option (aelf.csharp_state) = "AElf.Contracts.Greeter.GreeterContractState";

    // Actions
    rpc Greet (google.protobuf.Empty) returns (google.protobuf.StringValue) { }
    rpc GreetTo (google.protobuf.StringValue) returns (GreetToOutput) { }

    // Views
    rpc GetGreetedList (google.protobuf.Empty) returns (GreetedList) {
        option (aelf.is_view) = true;
    }
}

message GreetToOutput {
    string name = 1;
    google.protobuf.Timestamp greet_time = 2;
}

message GreetedList {
    repeated string value = 1;
}
```

Above is the full definition of the contract; it is mainly composed of three parts:
- **imports**: the dependencies of your contract.
- **the service definition**: the methods of your contract.
- **types**: some custom defined types used by the contract.

Let's have a deeper look at the three different parts.

#### Syntax, imports and namespace

```Protobuf 
syntax = "proto3";

import "aelf/options.proto";

import "google/protobuf/empty.proto";
import "google/protobuf/timestamp.proto";
import "google/protobuf/wrappers.proto";

option csharp_namespace = "AElf.Contracts.Greeter";
```

The first line specifies the syntax that this protobuf file uses, we recommend you always use **proto3** for your contracts. Next, you'll notice that this contract specifies some imports, let's analyze them briefly:
- **aelf/options.proto** : contracts can use AElf specific options; this file contains the definitions. One example is the **is_view** options that we will use later.
- **empty.proto, timestamp.proto and wrappers.proto** : these are proto files imported directly from protobuf's library. They are useful for defining things like an empty return value, time, and wrappers around some common types such as string. 

The last line specifies an option that determines the target namespace of the generated code. Here the generated code will be in the ```AElf.Contracts.Greeter``` namespace.


#### The service definition


```Protobuf
service GreeterContract { 
    option (aelf.csharp_state) = "AElf.Contracts.Greeter.GreeterContractState";

    // Actions
    rpc Greet (google.protobuf.Empty) returns (google.protobuf.StringValue) { }
    rpc GreetTo (google.protobuf.StringValue) returns (GreetToOutput) { }

    // Views
    rpc GetGreetedList (google.protobuf.Empty) returns (GreetedList) {
        option (aelf.is_view) = true;
    }
}
```

The first line here uses the ```aelf.csharp_state``` option to specify the name (full name) of the state class. This means that the state of the contract should be defined in the ```GreeterContractState``` class under the ```AElf.Contracts.Greeter``` namespace.

Next, two **action** methods are defined: ```Greet``` and ```GreetTo```. A contract method is defined by three things: the **method name**, the **input argument(s) type(s)** and the **output type**. For example, ```Greet``` requires that the input type is ```google.protobuf.Empty``` that is used to specify that this method takes no arguments and the output type will be a google.protobuf.StringValue is a traditional string. As you can see with the ```GreetTo``` method, you can use custom types as input and output of contract methods.

The service also defines a **view** method, that is, a method used only to query the contracts state, and that has no side effect on the state. For example, the definition of ```GetGreetedList``` uses the **aelf.is_view** option to make it a view method.

**Best practice:**
- use **google.protobuf.Empty** to specify that a method takes no arguments (import ```google/protobuf/empty.proto```).
- use **google.protobuf.StringValue** to use a string (import ```google/protobuf/wrappers.proto```).
- use the **aelf.is_view** option to create a view method (import ```aelf/options.proto```).
- use the **aelf.csharp_state** to specify the namespace of your contracts state (import ```aelf/options.proto```).

#### Custom types

```Protobuf
message GreetToOutput {
    string name = 1;
    google.protobuf.Timestamp greet_time = 2;
}

message GreetedList {
    repeated string value = 1;
}
```

The protobuf file also includes the definition of two custom types. The **GreetToOutput** is the type returned by the ```GreetTo``` method and ```GreetedList``` is the return type of the ```GetGreetedList``` view method. You'll notice the **repeated** keyword the ```GreetedList``` message. This is protobuf syntax to represent a collection.

**Best practice:**
- use **google.protobuf.Timestamp** to represent a point in time (import ```google/protobuf/timestamp.proto```).
- use **repeated** to represent a collection of items of the same type.

### Implementation

Previously we defined the contract in a protobuf file, now let's take a look at the implementation of the contract methods defined above. This section explains how to extend the generated code and implement the logic in your smart contract.

#### Project and generated code

Smart contracts in AElf are built with normal C# project files (csproj format). We highly recommend that you create a folder for your contract inside Boilerplate's **contract** folder and add the csproj in it:

<!--
Boilerplate
## contract
### AElf.Contracts.GreeterContract
#### AElf.Contracts.GreeterContract.csproj
### AElf.Contracts.SomeOtherContract
#### ...
-->
```
.
└── Boilerplate
    └── contract
        ├── AElf.Contracts.GreeterContract             // project folder
        │   └── AElf.Contracts.GreeterContract.csproj  // project file
        └── AElf.Contracts.SomeOtherContract
            └── ...
```

In order for the code generation to work, you'll have to add two elements to the csproj: ```IsContract```  and ```ContractCode``` like in the following code snippet:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        // ...
        <IsContract>true</IsContract>
    </PropertyGroup>

    <ItemGroup>
        <ContractCode Include="..\..\protobuf\greeter_contract.proto">
            <Link>Protobuf\Proto\greeter_contract.proto</Link>
        </ContractCode>
    </ItemGroup>

</Project>
```


Because we included this, the build process will use the ```greeter_contract.proto``` file to generate the code. After building, the complete project folder should look like this:

<!--
AElf.Contracts.Greeter
## Protobuf
### Generated
#### GreeterContract.c.cs // generated contract base
#### GreeterContract.g.cs // generated type definitions
### Protobuf
#### aelf
##### core.proto
##### options.proto
#### greeter_contract.proto
## GreeterContractState.cs  // Added by the contract author
## GreeterContract.cs       // Added by the contract author
## AElf.Contracts.Greeter.csproj
 -->


```
.
└── AElf.Contracts.Greeter
    ├── Protobuf
    │   ├── Generated
    │   │   ├── GreeterContract.c.cs // generated contract base
    │   │   └── GreeterContract.g.cs // generated type definitions
    │   └── Protobuf
    │       ├── aelf
    │       │   ├── core.proto
    │       │   └── options.proto
    │       └── greeter_contract.proto
    ├── GreeterContractState.cs  // Added by the contract author
    ├── GreeterContract.cs       // Added by the contract author
    └── AElf.Contracts.Greeter.csproj // The project file
```

As you can see in the above folder hierarchy, the **Generated** folder contains two generated files ".g.cs" and ".c.cs" The first contains the generated C# types that correspond to the custom types defined in the proto (here ```GreetToOutput``` and ```GreetedList```). The second contains C# types related to the contract service, as the base class for the contract ```GreeterContractBase``` (this file also contains other generated code that relates to the C# SDK, which is not explained in this tutorial).

Below are the files that contain the implementation of the smart contract (logic and state implementation). Remember that this code is not generated and needs to be created by the contract author.

#### Extend the generated code

After defining and generating the code from the definition, the contract author extends the generated code to implement the logic of his contract. Two files are presented here:
- **GreeterContract**: the actual implementation of the logic, it inherits from the contract base generated by protobuf.
- **GreeterContractState**: the state class that contains properties for reading and writing the state. This class inherits the `ContractState` class from the C# SDK.

```csharp 

// contract/AElf.Contracts.GreeterContract/GreeterContract.cs

using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Greeter
{
    public class GreeterContract : GreeterContractContainer.GreeterContractBase
    {
        public override StringValue Greet(Empty input)
        {
            Context.LogDebug(() => "Hello World!");
            return new StringValue {Value = "Hello World!"};
        }

        public override GreetToOutput GreetTo(StringValue input)
        {
            // Should not greet to empty string or white space.
            Assert(!string.IsNullOrWhiteSpace(input.Value), "Invalid name.");

            // State.GreetedList.Value is null if not initialized.
            var greetList = State.GreetedList.Value ?? new GreetedList();

            // Add input.Value to State.GreetedList.Value if it's new to this list.
            if (!greetList.Value.Contains(input.Value))
            {
                greetList.Value.Add(input.Value);
            }

            // Update State.GreetedList.Value by setting it's value directly.
            State.GreetedList.Value = greetList;

            Context.LogDebug(() => $"Hello {input.Value}!");

            return new GreetToOutput
            {
                GreetTime = Context.CurrentBlockTime,
                Name = input.Value.Trim()
            };
        }

        public override GreetedList GetGreetedList(Empty input)
        {
            return State.GreetedList.Value ?? new GreetedList();
        }
    }
} 
```

```csharp
// contract/AElf.Contracts.GreeterContract/GreeterContractState.cs

using AElf.Sdk.CSharp.State;
 
 namespace AElf.Contracts.Greeter
 {
    public class GreeterContractState : ContractState
    {
        public SingletonState<GreetedList> GreetedList { get; set; }
    }
 }
```

Let's briefly explain what is happening in the `GreetTo` method:

#### Asserting 

```csharp
Assert(!string.IsNullOrWhiteSpace(input.Value), "Invalid name.");
```

When writing a smart contract, it is often useful (and recommended) to validate the input. AElf smart contracts can use the ```Assert``` method defined in the base smart contract class to implement this pattern. For example, here, the method validates that the input string is null or composed only of white spaces. If the condition is false, this line will abort the execution of the transaction.

#### Accessing and saving state 

```csharp
var greetList = State.GreetedList.Value ?? new GreetedList();
...
State.GreetedList.Value = greetList;
```

From within the contract methods, you can easily access the contracts state through the ```State``` property of the contract. Here the state property refers to the ```GreeterContractState``` class in which is defined the ```GreetedList``` collection. The second effectively updates the state (this is needed; otherwise, the method would have no effect on the state).

**Note** that because the ```GreetedList``` type is wrapped in a ```SingletonState``` you have to use the ```Value``` property to access the data (more on this later).

#### Logging

```csharp
Context.LogDebug(() => $"Hello {input.Value}!");
```

It is also possible to log from smart contract methods. The above example will log "Hello" and the value of the input. It also prints useful information like the ID of the transaction. 

#### More on state

As a reminder, here is the state definition in the contract (we specified the name of the class and a type) as well as the custom type ```GreetedList```:
```protobuf
service GreeterContract { 
    option (aelf.csharp_state) = "AElf.Contracts.Greeter.GreeterContractState";
    ... 
}

// ...

message GreetedList {
    repeated string value = 1;
}

```

The ```aelf.csharp_state``` option allows the contract author to specify in which namespace and class name the state will be. To implement a state class, you need to inherit from the ```ContractState``` class that is contained in the C# SDK (notice the ```using``` statement here below).

Below is the state class that we saw previously:
```csharp
using AElf.Sdk.CSharp.State;
 
 namespace AElf.Contracts.Greeter
 {
    public class GreeterContractState : ContractState
    {
        public SingletonState<GreetedList> GreetedList { get; set; }
    }
 }
```

The state uses the custom ```GreetedList``` type, which was generated from the Protobuf definition at build time and contained exactly one property: a singleton state of type  ```GreetedList```.

The ```SingletonState``` is part of the C# SDK and is used to represent exactly **one** value. The value can be of any type, including collection types. Here we only wanted our contract to store one list (here a list of strings).

**Note** that you have to wrap your state types in a type like ```SingletonState``` (others are also available like ```MappedState```) because behind the scene, they implement the state read and write operations.

## Next 

This article showed you how to set up the contract's definition and implementation, next, we'll see how to test it.