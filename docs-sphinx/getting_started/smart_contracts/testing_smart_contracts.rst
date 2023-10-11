Testing Smart Contracts
=======================

GreeterContract Testing
-----------------------

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
^^^^^^^^^^^^^^^^^^^^^

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
^^^^^^^^^^^^^^^^^^^^^^^^^^

To test contract methods locally, we need to set up the context required for testing, which primarily involves 
getting the stub for the contract. Here is the content of _Setup.cs file.

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

TestBase inherits ContractTestBase<Module>. Then we define a contract stub under the class, and get a keyPair from the 
aelf contract TestKit framework. Later, in the constructor, we input the address and keyPair parameters and use the GetTester method to get the contract stub.

Implement contract unit test code
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

Now for the easy part, the test class only needs to inherit from the TestBase. After this, you can write unit test implementations you need.

Here, we can call the Update method to save the message to the state first, then call the Read method to retrieve the message from the state. 
Finally, we compare the retrieved message with the message inputed to check whether the values are the same.

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