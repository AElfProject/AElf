Creating Smart Contracts
========================

This article will guide you through how to use **AElf Boilerplate** to
implement a smart contract. It takes an example on the **Greeter**
contract that’s already included in Boilerplate. Based on the concepts
this article presents, you’ll be able to create your own basic contract.

This section will guide you through how to use **AElf-Tools** and **Template** to 
implement a smart contract. We will use **GreeterContract** as an example 
to show how to develop a simple contract base on it. Meanwhile, introduce concepts of
the aelf smart contracts to you.

**Steps of creating smart contracts**

The following content will walk you through the basics of writing a
smart contract; this process contains essentially four steps:

-  **Install template**: install the aelf smart contract template 
   locally using ``dotnet`` command.

-  **Initialize project**: build the project structure and generate 
   the base contract code from the proto definition with ``dotnet`` command.
   
-  **defining the contract**: the methods and types needed in your
   contract should be defined in a protobuf file, following typical
   protobuf syntax.

-  **Implement contract code**: implement the logic of the contract
   methods.

The ``Greeter`` contract is a very simple contract that exposes a
``AddGreeters`` method to add a new greeter to GreetList, and a 
``GetGreeters`` method to get all of greeters.

This tutorial shows you how to develop a smart contract with the C#
contract SDK; you can find you more
`here <https://docs.aelf.io/en/latest/reference/contract-sdk/index.html>`__.
AElf-Tools and Template will automatically add the reference to the SDK.

Install template
----------------

Installing template means to download templates from nuget repo to your local 
environment, and install it locally. Run the following command to install it.

::

    dotnet new install AElf.ContractTemplates

After installing, you can use dotnet new uninstall to check this template on local.

::

    Currently installed items:
       AElf.ContractTemplates
          Version: 1.0.0-alpha
          Details:
             Author: AElf
             NuGetSource: https://api.nuget.org/v3/index.json
          Templates:
             AElf Contract (aelf) C#
          Uninstall Command:
             dotnet new uninstall AElf.ContractTemplates
         
If you can see this result, it means that the template installation is successful. 
These information show the template name, version and other details.

Initialize project
------------------

After installing, we need to initialize the project. Initializing the project is like 
creating a specific contract project based on the template. This process is similar to 
using the ``new()`` method in OOP to create an instance of a class.

Using ``dotnet new`` command to create a specific contract project. We can create custom contract 
projects based on the template using ``-n`` and ``-N`` options. And ``-n`` stands for contract name, ``-N`` stands for namespace.

Run the following command, you can create a contract project that named ``GreeterContract``. In this case, 
the contract name will be ``GreeterContract``. And the namespace of the project will be ``AElf.Contracts.Greeter``.

::

    dotnet new aelf -n GreeterContract -N AElf.Contracts.Greeter
      
    After running dotnet new command, we can get a new project generated base on template. 
    The project structure is as follows.

::

    .
    ├── src
    │   ├── GreeterContract.cs
    │   ├── GreeterContract.csproj
    │   ├── GreeterContractState.cs
    │   └── Protobuf
    │       ├── contract
    │       │   └── hello_world_contract.proto
    │       └── message
    │           └── authority_info.proto
    └── test
        ├── GreeterContract.Tests.csproj
        ├── GreeterContractTests.cs
        ├── Protobuf
        │   ├── message
        │   │   └── authority_info.proto
        │   └── stub
        │       └── hello_world_contract.proto
        └── _Setup.cs

The src folder
^^^^^^^^^^^^^^

The src folder contains several proto files used to describe blockchain smart contract methods 
and data structures. It also includes specific implementations of smart contract methods and 
definition files for managing contract state in communication with the blockchain, such as GreeterContractState.cs here.

::

    src
    ├── GreeterContract.cs
    ├── GreeterContract.csproj
    ├── GreeterContractState.cs
    └── Protobuf

The test folder
^^^^^^^^^^^^^^^

The test folder similarly contains a proto subfolder, along with a setup file used to establish 
the unit testing environment for blockchain smart contracts. It defines test module classes and a base test class, 
facilitating context loading, stub class retrieval, and stub acquisition methods. As a result, these classes and 
methods are employed in unit tests to conduct various tests on the smart contract.

::

    src
    ├── _Setup.cs
    ├── GreeterContract.Tests.csproj
    ├── GreeterContractTests.cs
    └── Protobuf


Defining the contract
---------------------

AElf defines smart contracts as services that are implemented using gRPC and Protobuf. These definitions are placed 
in the proto files and do not contain logic. The proto files are used to generate C# classes that will be used to 
implement the logic and state of the contract.

In the Protobuf folder, the different folders are used to store different definition proto files 
(If there is no corresponding folder, you can create it yourself. Only the contract and message directories are used here).
For Protobuf under the src folder:
- contract: the contract folder is used to store definition proto file of contract.
- message: the proto files under the message folder are used to define some common properties for import and use by other proto files.
- reference: the reference folder is used to store the proto files of the referenced contract.
- base: the reference folder is used to store the basic proto files, such as ACS (aelf standard contract) proto files.

::

    src
    └── Protobuf
        ├── contract
        │   └── hello_world_contract.proto
        └── message
            └── authority_info.proto

The **hello_world_contract.proto** file is used for the HelloWorld contract template. We need to delete this proto file first.
Then, create a new greet_contract.proto file, which will be used for the GreetContract contract. Let's see how to write definitions 
in the proto file.

.. code:: protobuf

    syntax = "proto3";
    
    import "aelf/options.proto";
    import "google/protobuf/empty.proto";
    import "google/protobuf/wrappers.proto";
    // The namespace of this class
    option csharp_namespace = "AElf.Contracts.Greeter";
    
    service GreeterContract {
      // The name of the state class the smart contract is going to use to access blockchain state
      option (aelf.csharp_state) = "AElf.Contracts.Greeter.GreeterContractState";
    
      // Actions (methods that modify contract state)
      // Stores the value in contract state
      rpc AddGreeters (google.protobuf.StringValue) returns (google.protobuf.Empty) {
      }
    
      // Views (methods that don't modify contract state)
      // Get the value stored from contract state
      rpc GetGreeters (google.protobuf.Empty) returns (GreeterList) {
        option (aelf.is_view) = true;
      }
    }
    message GreeterList {
        repeated string greeter = 1;
    }

Above is the full definition of the contract, it is mainly composed of three parts:
- imports: the dependencies of your contract.
- service definitions: the methods of your contract.
- types: some custom defined types used by the contract.
Let’s have a deeper look at the three different parts.

Syntax, imports and namespace
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

.. code:: protobuf

    syntax = "proto3";
    
    import "aelf/options.proto";
    import "google/protobuf/empty.proto";
    import "google/protobuf/wrappers.proto";
    // The namespace of this class
    option csharp_namespace = "AElf.Contracts.Greeter";

The first line specifies the syntax that this protobuf file uses, we recommend you always use proto3 for your contracts. 
Next, you’ll notice that this contract specifies some imports, let’s describe them briefly:
- aelf/options.proto : contracts can use AElf specific options. This file contains the definitions. One example is the is_view options that we will use later.
- empty.proto, wrappers.proto : these are proto files imported directly from protobuf’s library. They are useful for defining things like an empty return value and wrappers around some common types such as string.
The last line specifies an option that determines the target namespace of the generated code. Here the generated code will be in the ``AElf.Contracts.Greeter`` namespace.

Service definitions
^^^^^^^^^^^^^^^^^^^

.. code:: protobuf

    service GreeterContract {
      // The name of the state class the smart contract is going to use to access blockchain state
      option (aelf.csharp_state) = "AElf.Contracts.Greeter.GreeterContractState";
    
      // Actions (methods that modify contract state)
      // Stores the value in contract state
      rpc AddGreeters (google.protobuf.StringValue) returns (google.protobuf.Empty) {
      }
    
      // Views (methods that don't modify contract state)
      // Get the value stored from contract state
      rpc GetGreeters (google.protobuf.Empty) returns (GreeterList) {
        option (aelf.is_view) = true;
      }
    }

The first line here uses the ``aelf.csharp_state`` option to specify the name (full name) of the state class. 
This means that the state of the contract should be defined in the ``GreeterContractState`` class under the ``AElf.Contracts.Greeter`` namespace.

Next, an action method is defined: ``AddGreeters``. A contract method is defined by three parts: the method name, 
the input argument(s) type(s) and the output type. For example, ``AddGreeters`` requires that the input type is ``google.protobuf.StringValue`` 
that is used to specify that this method takes an argument and the output type will be ``google.protobuf.Empty``.

Then a view method is also defined in the service: ``GetGreeters``. The method is used only to query the contract state, 
and that has no side effect on the state. The definition of ``GetGreeters`` uses the aelf.is_view option to make it a view method.

A brief summary follows.
- use google.protobuf.Empty to specify that a method takes no arguments (import google/protobuf/empty.proto).
- use google.protobuf.StringValue to use a string (import google/protobuf/wrappers.proto).
- use the aelf.is_view option to create a view method (import aelf/options.proto).
- use the aelf.csharp_state to specify the namespace of your contracts state (import aelf/options.proto).

Custom types
^^^^^^^^^^^^

.. code:: protobuf

    message GreeterList {
        repeated string greeter = 1;
    }

# todo
The protobuf file also includes the definition of a custom type. The GreeterList is an event type (you can also define a normal type, normal type has no aelf.is_event). Event types can be used to emit an event during the execution of a contract. Normal types are used as types for input and output parameters.

A brief summary follows.
- use the aelf.is_event option to specify that the type will emit an event.
- use repeated to represent a collection of items of the same type.


Implement contract code
-----------------------

After defining definitions, we need to run dotnet build command again under the src folder so that 
recompile these proto files you defined and generate new C# code. And we can run this command again 
after each modification of the definition to compile and generate the latest code.

For now, we can extend the generated code to implement the logic of the contract. Two files are presented here:
- GreeterContract: the actual implementation logic, it inherits from the contract base generated by proto files.
- GreeterContractState: the state class that contains properties for reading and writing the state. This class inherits the ContractState class from the C# SDK.

.. code:: csharp

    using AElf.Sdk.CSharp;
    using Google.Protobuf.WellKnownTypes;
    
    namespace AElf.Contracts.Greeter
    {
        // Contract class must inherit the base class generated from the proto file
        public class GreeterContract : GreeterContractContainer.GreeterContractBase
        {
            // A method that modifies the contract state
            public override Empty AddGreeters(StringValue input)
            {
                // Should not greet to empty string or white space.
                Assert(!string.IsNullOrWhiteSpace(input.Value), "Invalid name.");
    
                // State.GreetedList.Value is null if not initialized.
                var greeterList = State.GreeterList.Value ?? new GreeterList();
    
                // Add input.Value to State.GreetedList.Value if it's new to this list.
                if (!greeterList.Greeter.Contains(input.Value))
                {
                    greeterList.Greeter.Add(input.Value);
                }
                
                // Update State.GreetedList.Value by setting it's value directly.
                State.GreeterList.Value = greeterList;
                
                return new Empty();
            }
    
            // A method that read the contract state
            public override GreeterList GetGreeters(Empty input)
            {
                return State.GreeterList.Value ?? new GreeterList();
            }
        }   
    }

.. code:: csharp

    using AElf.Sdk.CSharp.State;
    
     namespace AElf.Contracts.Greeter
     {
        public class GreeterContractState : ContractState
        {
            public SingletonState<GreeterList> GreeterList { get; set; }
        }
     }
 
Asserting
^^^^^^^^^

.. code:: csharp

    Assert(!string.IsNullOrWhiteSpace(input.Value), "Invalid name.");

When writing a smart contract, it is often useful (and recommended) to
validate the input. AElf smart contracts can use the ``Assert`` method
defined in the base smart contract class to implement this pattern. For
example, here, the method validates that the input string is null or
composed only of white spaces. If the condition is false, this line will
abort the execution of the transaction.

Saving and reading state
^^^^^^^^^^^^^^^^^^^^^^^^

.. code:: csharp

    State.GreeterList.Value = greeterList;
    ...
    var greeterList = State.GreeterList.Value;

From within the contract methods, you can easily save and read the contracts state through the 
State property of the contract. Here the state property refers to the GreeterContractState class. 
The first one is to save the input value to the state, and the second one is to read the value from the state.

Contract state
^^^^^^^^^^^^^^

As a reminder, here is the state definition in the contract (we
specified the name of the class and a type) as well as the custom type
``GreeterList``:

.. code:: csharp

    public class GreeterContractState : ContractState
    {
        public SingletonState<GreeterList> GreeterList { get; set; }
    }

The aelf.csharp_state option allows the contract author to specify in which namespace and class name the state will be. 
To implement a state class, you need to inherit from the ContractState class that is contained in the C# SDK.
For the type of properties under the state. We have a generic way of writing.
- For save and read a single object: use SingletonState<ClassType>.
- For save and read a key-value pair: use MappedState<KeyClassType, ValueClassType>
We also can use StringState instead of SingletonState<ClassType> after we are familiar with all the usages of the state.
