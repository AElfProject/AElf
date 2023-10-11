Testing Smart Contracts
=======================

GreeterContract Testing
-----------------

The aelf-developer-tools greatly simplifies unit tests. This tutorial will test the HelloWorld contract for reference.

AElf Contract TestKit is a testing framework specifically used to test AElf smart contracts. With this framework,
you can simulate the execution of a transaction by constructing a stub of a smart contract and using the methods provided 
by the Stub instance (corresponding to the contract's Actions methods) and query (corresponding to the Views methods of the contract), 
and then get the transaction execution results in the test case.

As you can see, test code is placed in the test folder. This test folder usually contains a project file (.csproj) and at least two .cs files. 
The project file is a basic C# xUnit test project file, to which we've added some references.

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
The testing process is quite similar to the development process.
- Defining the contract: the methods and types needed in your contract should be defined in a protobuf file, they are same as the src. We can simply copy them to test folders.
- Setting up testing context: to test contracts locally, we need to simulate the execution of a transaction by constructing a stub. Here, we will set up some context and stub required for testing.
- Implement contract unit test code: implement the logic of unit test methods.

Defining the contract
^^^^^^^^^^^^^^^^^^

The Protobuf folder under the test directory serves the same purpose as the src directory, but with slightly different folder names.
For Protobuf under the test folder:
- message: the proto files under the message folder are used to define some common properties for import and use by other proto files.
- stub: the stub folder contains contract proto files for unit testing, as well as other proto files that are dependent on and imported into this test proto file.

::
test
└── Protobuf
    ├── message
    │   └── authority_info.proto
    └── stub
        └── hello_world_contract.proto

We just need to copy these proto files of the src folder and paste them here. Please note that the contract proto files 
in the src folder and other proto files that are dependencies need to be placed in the stub folder.

Setting up testing context
^^^^^^^^^^^^^^^^^^

To test contract methods locally, we need to set up the context required for testing, which primarily involves 
getting the stub for the contract.

.. code:: csharp
namespace AElf.Contracts.HelloWorld
{
    // The Module class load the context required for unit testing
    public class Module : ContractTestModule<HelloWorld>
    {
    }
    // The TestBase class inherit ContractTestBase class, it defines Stub classes and gets instances required for unit testing
    public class TestBase : ContractTestBase<Module>
    {
        // The Stub class for unit testing
        internal readonly HelloWorldContainer.HelloWorldStub HelloWorldStub;
        // A key pair that can be used to interact with the contract instance
        private ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;

        public TestBase()
        {
            HelloWorldStub = GetHelloWorldContractStub(DefaultKeyPair);
        }
        private HelloWorldContainer.HelloWorldStub GetHelloWorldContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<HelloWorldContainer.HelloWorldStub>(ContractAddress, senderKeyPair);
        }
    }   
}

TestBase inherits ContractTestBase<Module>. Then we define a contract stub under the class, and get a keyPair from the 
aelf contract TestKit framework. Later, in the constructor, we input the address and keyPair parameters and use the GetTester method to get the contract stub.

Implement contract unit test code
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Now for the easy part, the test class only needs to inherit from the TestBase. After this, you can write unit test implementations you need.

Here, we can call the Update method to save the message to the state first, then call the Read method to retrieve the message from the state. 
Finally, we compare the retrieved message with the message inputed to check whether the values are the same.

.. code:: csharp
public class HelloWorldTests : TestBase
{
    [Fact]
    public async Task Update_ShouldUpdateMessageAndFireEvent()
    {
        // Arrange
        var inputValue = "Hello, World!";
        var input = new StringValue { Value = inputValue };
        // Act
        await HelloWorldStub.Update.SendAsync(input);
        // Assert
        var updatedMessage = await HelloWorldStub.Read.CallAsync(new Empty());
        updatedMessage.Value.ShouldBe(inputValue);
    }
}