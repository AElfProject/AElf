Creating Smart Contracts
========================

This article will guide you on how to use AElf-developer-tools to implement a smart contract.
It uses the **GreeterContract** as an example, which is already included in contract templates.
With the concepts presented in this article, you will be able to create your own basic contract.

In this section, you will be guided on how to use AElf-Tools and Templates to implement a smart contract.
We will use the **GreeterContract** as an example to demonstrate how to develop a simple contract based on it.
We will also introduce you to the concepts of aelf smart contracts.

**Steps for developing smart contracts**

The following content will walk you through the basics of writing a
smart contract; this process contains essentially four steps:

-  **Install template**: Install the aelf smart contract templates
   locally using the dotnet command.

-  **Initialize project**: Build the project structure and generate 
   the base contract code from the proto definition using the dotnet command.
   
-  **Define the contract**: The methods and types required in your contract 
   should be defined in a protobuf file following the typical protobuf syntax.

-  **Implement contract code**: implement the logic for the contract
   methods.

The ``Greeter`` contract is a very simple contract that exposes an
``AddGreeters`` method to add a new greeter to ``GreeterList``, and a 
``GetGreeters`` method to get all of greeters.

This tutorial demonstrates how to develop a smart contract using the C# contract SDK.
You can learn more about it
`here <https://docs.aelf.io/en/latest/reference/contract-sdk/index.html>`__.
AElf-developer-tools and contract templates will automatically add the reference to the SDK.

Install template
----------------

Installing a template means downloading templates from the NuGet repository to your local environment 
and installing them locally. Run the following command to install it.

::

    dotnet new install AElf.ContractTemplates

After installation, you can use 'dotnet new uninstall' to verify the presence of this template locally.

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
         
If you can see this result, it indicates that the template installation was successful. 
This information shows the template name, version, and other details.

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

The **src** folder contains several protobuf files used to describe blockchain smart contract methods 
and data structures. It also includes specific implementations of smart contract methods and 
definition files for managing contract state in communication with the blockchain. For example, GreeterContractState.cs is one such file.

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

Similarly, the **test** folder contains a proto subfolder, along with a setup file used to establish 
the unit testing environment for blockchain smart contracts. It defines test module classes and a base test class, 
facilitating context loading, stub class retrieval, and stub acquisition methods. These classes and methods are 
employed in unit tests to conduct various tests on the smart contract.

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

In the Protobuf folder, different subfolders are used to store various definition proto files. 
If a corresponding folder does not exist, you can create one yourself. In this context, 
only the contract and message directories are used. Here's a breakdown of the Protobuf content under the src folder:

- contract: the contract folder is used to store definition proto file of contract.
- message: the proto files under the message folder are used to define common properties for import and use by other proto files.
- reference: the reference folder is used to store the proto files of the referenced contract.
- base: the base folder is used to store the basic proto files, such as ACS (aelf standard contract) proto files.

::

    src
    └── Protobuf
        ├── contract
        │   └── hello_world_contract.proto
        └── message
            └── authority_info.proto

The **hello_world_contract.proto** file is used as a template for the HelloWorld contract. 
First, we need to delete this proto file. Next, we will create a new **greeter_contract.proto** file, 
which will be used for the GreeterContract contract. Let's explore how to write definitions in the proto file.

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

The complete contract definition consists of three main parts:

- Imports: These are the dependencies of your contract.
- Service Definitions: These define the methods of your contract.
- Types: These are custom-defined types used by the contract.

Now, let's take a closer look at these three different parts.

Syntax, imports and namespace
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

.. code:: protobuf

    syntax = "proto3";
    
    import "aelf/options.proto";
    import "google/protobuf/empty.proto";
    import "google/protobuf/wrappers.proto";
    // The namespace of this class
    option csharp_namespace = "AElf.Contracts.Greeter";

The first line specifies the syntax used in this protobuf file. We recommend using proto3 for your contracts. 
Next, you will notice that this contract specifies some imports. Let's briefly describe them:

- aelf/options.proto: Contracts can use AElf-specific options. This file contains the definitions, including options like is_view that we will use later.
- empty.proto, wrappers.proto: These are proto files imported directly from the protobuf library. They are useful for defining things like an empty return value and wrappers around common types, such as strings.

The last line specifies an option that determines the target namespace of the generated code. In this case, 
the generated code will be placed in the AElf.Contracts.Greeter namespace.

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

In the first line, we use the ``aelf.csharp_state`` option to specify the full name of the state class. 
This indicates that the state of the contract should be defined in the ``GreeterContractState`` class under the ``AElf.Contracts.Greeter`` namespace.

Next, an action method is defined: ``AddGreeters``. A contract method is composed of three parts: the method name, 
the input argument type(s), and the output type. For instance, ``AddGreeters`` specifies that it requires a ``google.protobuf.StringValue`` 
input type, indicating that this method takes an argument, and the output type will be ``google.protobuf.Empty``.

The service also defines a view method: ``GetGreeters``. This method is exclusively used to query the contract state 
and has no side effects on the state. The definition of ``GetGreeters`` uses the ``aelf.is_view`` option to designate it as a view method.

To summarize:

- Use google.protobuf.Empty to specify that a method takes no arguments (import google/protobuf/empty.proto).
- Use google.protobuf.StringValue to handle strings (import google/protobuf/wrappers.proto).
- Use the aelf.is_view option to create a view method (import aelf/options.proto).
- Use the aelf.csharp_state option to specify the namespace of your contract's state (import aelf/options.proto)."

Custom types
^^^^^^^^^^^^

.. code:: protobuf

    message GreeterList {
        repeated string greeter = 1;
    }

A brief summary follows:

- Use the aelf.is_event option to indicate that the type will trigger an event.
- Use **repeated** to denote a collection of items of the same type.

Implement contract code
-----------------------

After defining the contract's structure and methods, you need to execute the dotnet build command within the src folder. 
This will recompile the proto files and generate updated C# code. You should repeat this command every time you make changes 
to the contract's structure to ensure the code is up to date.

Currently, you can extend the generated code to implement the contract's logic. There are two key files involved:

- GreeterContract: This file contains the actual implementation logic. It inherits from the contract base generated by the proto files.
- GreeterContractState: This is the state class that holds properties for reading and writing the contract's state. It inherits the ContractState class from the C# SDK.

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

When writing a smart contract, it is often useful and recommended to validate the input. AElf smart contracts can utilize 
the ``Assert`` method defined in the base smart contract class to implement this pattern. For example, in the following method, 
validation checks if the input string is null or consists only of white spaces. If this condition evaluates to false, 
the transaction execution will be terminated.

Saving and reading state
^^^^^^^^^^^^^^^^^^^^^^^^

.. code:: csharp

    State.GreeterList.Value = greeterList;
    ...
    var greeterList = State.GreeterList.Value;

From within the contract methods, you can easily save and read the contract's state using the State property of the contract. 
In this context, the State property refers to the GreeterContractState class. The first line is used to save the input value to the state, 
while the second line is used to retrieve the value from the state.

Contract state
^^^^^^^^^^^^^^

As a reminder, here is the state definition in the contract where we specify the name of the class and its type, 
along with the custom type ``GreeterList``:

.. code:: csharp

    public class GreeterContractState : ContractState
    {
        public SingletonState<GreeterList> GreeterList { get; set; }
    }

The aelf.csharp_state option allows the contract author to specify the namespace and class name for the state. 
To implement a state class, you need to inherit from the ContractState class provided by the C# SDK. 
When defining properties under the state, we follow a generic approach:

- To save and read a single object: use SingletonState<ClassType>.
- To save and read a key-value pair: use MappedState<KeyClassType, ValueClassType>.

After becoming familiar with all state usages, you can also use StringState as an alternative to SingletonState<ClassType>.