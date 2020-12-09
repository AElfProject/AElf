# Unit testing a contract

The previous article exposed how to add the proto definition and implement the logic of your contract. This article expands on the previous and will show you how to test your contract.

AElf Contract TestKit is a testing framework specifically used to test AElf smart contracts. With this framework, you can simulate the execution of a transaction by constructing a stub of a smart contract and using the methods provided by the Stub instance (corresponding to the contract's Action methods) and query (corresponding to the View methods of the contract), and then get the transaction execution results in the test case.

## Test project

**AElf Boilerplate**'s code generator has automatically generated test project for you, you just need to add your test cases.

As you can see, tests are placed in the **test** folder. Each test folder usually contains a project file (.csproj) and at least four .cs files. The project file is a basic C# xUnit test project file, to which we've added some references.

<!-- 
# chain 
## contract
## protobuf
## src
## test
### AElf.Contracts.GreeterContract.Tests
#### AElf.Contracts.GreeterContract.Tests.csproj
#### GreeterContractTestBase.cs
#### GreeterContractTestModule.cs
#### GreeterContractTests.cs
#### GreeterContractInitializationProvider.cs
### ...
-->

```
.
└── chain
    ├── contract
    ├── protobuf
    ├── src
    └── test
        ├── AElf.Contracts.GreeterContract.Tests
        │   ├── AElf.Contracts.GreeterContract.Tests.csproj // xUnit test project
        │   ├── GreeterContractTestBase.cs
        │   ├── GreeterContractTestModule.cs
        │   └── GreeterContractTests.cs
        │   └── GreeterContractInitializationProvider.cs
        └── ...
```

### Test your contract

Now for the easy part, the test class only needs to inherit from the test base. After this you can go ahead and create the test cases you need.

**GreeterContractTest.cs**
```csharp
public class GreeterContractTests : GreeterContractTestBase
{
    // declare the method as a xUnit test method
    [Fact]
    public async Task GreetTest()
    {
        // Use the contracts stub to call the 'Greet' method and get a reference to 
        // the transaction result.
        var txResult = await GetGreeterContractStub(_defaultKeyPair).Greet.SendAsync(new Empty());
            
        // check that the transaction was mined
        txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
        // parse the result (return from the contract)
        var text = new StringValue();
        text.MergeFrom(txResult.TransactionResult.ReturnValue);
            
        // check that the value is correct
        text.Value.ShouldBe("Hello World!");
    }

    // ...
}
```

From the previous code snippet you can note several things:
- the test case is a classic xUnit test class.
- you can use the contracts stub to call the contract and check returns.

Feel free to have a look at the full test class in the Boilerplate source code.

## Next

We've seen how to add the tests for a contract. The next step is to run the node where the contract is deployed to actually be able to interact with the contract from external sources.