Testing Smart Contracts
=======================

GreeterContract Testing
-----------------------

The aelf-developer-tools greatly simplify unit tests. This tutorial will demonstrate how to test the GreeterContract for reference.

AElf Contract TestKit is a testing framework designed specifically for testing AElf smart contracts. With this framework, 
you can simulate the execution of a transaction by constructing a stub of a smart contract and utilize the methods provided 
by the Stub instance (corresponding to the contract's Action methods) for executing transactions and 
queries (corresponding to the Views methods of the contract) to obtain transaction execution results in the test case.

As you can observe, the test code is located within the test folder. Typically, this test folder contains a project file (.csproj) 
and at least two .cs files. The project file serves as a standard C# xUnit test project file, with additional references included as needed.

::

    test
    ├── GreeterContract.Tests.csproj
    ├── GreeterContractTests.cs
    ├── Protobuf
    │   ├── message
    │   │   └── authority_info.proto
    │   └── stub
    │       └── hello_world_contract.proto
    └── _Setup.cs

**Steps of testing smart contracts**
The testing process closely mirrors the development process and generally consists of the following steps:

- Defining the Contract: All the required methods and types for your contract should be defined in a protobuf file. These definitions are identical to those in the src folder, and you can simply copy them to the test folder.
- Setting up the Testing Context: To conduct local contract testing, it's essential to simulate the execution of a transaction by creating a stub. In this step, you will configure the necessary context and stub components needed for testing.
- Implementing Contract Unit Test Code: Create the logic for unit test methods, which will test the contract's functionality and ensure it works as expected.

Defining the contract
^^^^^^^^^^^^^^^^^^^^^

The Protobuf folder within the test directory serves a similar purpose to the src directory but with slightly different folder names. 
For the Protobuf section within the test folder, the following applies:

- message: The proto files contained in the message folder are used to define common properties that can be imported and utilized by other proto files.
- stub: The stub folder houses contract proto files dedicated to unit testing. Additionally, it may contain other proto files that this test proto file depends on and imports.

::

    test
    └── Protobuf
        ├── message
        │   └── authority_info.proto
        └── stub
            └── hello_world_contract.proto

You can copy the necessary proto files from the src folder and paste them into the stub folder. It's important to ensure that 
contract proto files from the src folder and any dependent proto files are correctly placed in the stub directory.

Setting up testing context
^^^^^^^^^^^^^^^^^^^^^^^^^^

To locally test contract methods, you need to establish the context required for testing. This process primarily 
involves obtaining the stub for the contract. Below is the content of the **_Setup.cs** file:

.. code:: csharp

    using AElf.Cryptography.ECDSA;
    using AElf.Testing.TestBase;
    
    namespace AElf.Contracts.Greeter
    {
        // The Module class load the context required for unit testing
        public class Module : ContractTestModule<GreeterContract>
        {
        }
        // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
        public class TestBase : ContractTestBase<Module>
        {
            // The Stub class for unit testing
            internal readonly GreeterContractContainer.GreeterContractStub GreeterContractStub;
            // A key pair that can be used to interact with the contract instance
            private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    
            public TestBase()
            {
                GreeterContractStub = GetGreeterContractContractStub(DefaultKeyPair);
            }
            private GreeterContractContainer.GreeterContractStub GetGreeterContractContractStub(ECKeyPair senderKeyPair)
            {
                return GetTester<GreeterContractContainer.GreeterContractStub>(ContractAddress, senderKeyPair);
            }
        }   
    }

In this code, TestBase inherits ContractTestBase<Module> and defines a contract stub within the class. 
It also obtains a key pair from the AElf contract TestKit framework. In the constructor, the address and 
key pair parameters are provided, and the "GetTester" method is used to retrieve the contract stub.

Implement contract unit test code
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Now comes the easy part: the test class only needs to inherit from TestBase. Once you've done that, 
you can proceed to write the unit test implementations you require.

In this section, you can use the ``AddGreetersTest`` method to save a message to the state. Following that, 
you can call the ``GetGreeters`` method to retrieve the message from the state. Finally, you can compare the retrieved message 
with the originally input message to verify whether the values match.

.. code:: csharp

    using System.Threading.Tasks;
    using Google.Protobuf.WellKnownTypes;
    using Shouldly;
    using Xunit;
    
    namespace AElf.Contracts.Greeter
    {
        // This class is unit test class, and it inherit TestBase. Write your unit test code inside it
        public class GreeterContractTests : TestBase
        {
            [Fact]
            public async Task AddGreetersTest()
            {
                // Arrange
                var user1 = new StringValue { Value = "Tom" };
                var user2 = new StringValue { Value = "Jerry" };
                var expectList = new GreeterList();
                expectList.Greeter.Add(user1.Value);
                expectList.Greeter.Add(user2.Value);
    
                // Act
                await GreeterContractStub.AddGreeters.SendAsync(user1);
                await GreeterContractStub.AddGreeters.SendAsync(user2);
    
                // Assert
                var greeterList = await GreeterContractStub.GetGreeters.CallAsync(new Empty());
                greeterList.ShouldBe(expectList);
            }
        }
    }