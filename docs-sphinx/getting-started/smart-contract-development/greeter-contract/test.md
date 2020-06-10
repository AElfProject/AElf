# Unit testing a contract

The previous article exposed how to add the proto definition and implement the logic of your contract. This article expands on the previous and will show you the following based on the Greeter contract:
- how to create the test project.
- using AElf Contract TestKit to test.
- adding the test module, test base, and test cases.

AElf Contract TestKit is a testing framework specifically used to test AElf smart contracts. With this framework, you can simulate the execution of a transaction by constructing a stub of a smart contract and using the methods provided by the Stub instance (corresponding to the contract's Action methods) and query (corresponding to the View methods of the contract), and then get the transaction execution results in the test case.

## Creating the project

Before creating the project let's take a look at Boilerplate's structure concerning the tests:

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
        └── ...
```

As you can see, tests are placed in the **test** folder. Each test folder usually contains a project file (.csproj) and at least three .cs files. The project file is a basic C# xUnit test project file, to which we've added some references.

**AElf.Contracts.GreeterContract.Tests.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <!-- The same root namespace as the implementation project -->
        <RootNamespace>AElf.Contracts.GreeterContract</RootNamespace>
    </PropertyGroup>

    <ItemGroup>
        <!-- ... -->
        <PackageReference Include="xunit" Version="2.4.1" />
        <PackageReference Include="xunit.runner.console" Version="2.4.1" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.4.1" />
    </ItemGroup>

    <!-- Reference AElf TestKit -->
    <ItemGroup>
        <PackageReference Include="AElf.Contracts.TestKit" Version="0.9.0" />
    </ItemGroup>

    <ItemGroup>

        <!-- Need ACS0 for deployment-->
        <ContractStub Include="..\..\protobuf\acs0.proto">
            <Link>Protobuf\Proto\acs0.proto</Link>
        </ContractStub>

        <!-- Reference the contract proto definition -->
        <ContractStub Include="..\..\protobuf\greeter_contract.proto">
            <Link>Protobuf\Proto\greeter_contract.proto</Link>
        </ContractStub>

    </ItemGroup>

    <ItemGroup>
        <!-- Reference the contract implementation -->
        <ProjectReference Include="..\..\contract\AElf.Contracts.GreeterContract\AElf.Contracts.GreeterContract.csproj" />
    </ItemGroup>

</Project>
```

From the previous code snippet you can note several things:
- the root namespace corresponds to the root namespace in the contract implementation (in the .csproj).
- it's a standard c# project that references xUnit packages.
- it contains a reference to TestKit (be sure to reference the latest version).
- a reference to acs0 contract stub is added because when the test environment is initialized, the Greeter contract needs to be deployed, and this needs a reference to an ACS 0 (genesis contract).
- a reference to the Greeter contracts stub.

## Adding the files

### Test Module

A module is a unit used for modular management of the code by the ABP framework. AElf mostly follows the same code philosophy. Most modules only contain minimal logic:

**GreeterContractTestModule.cs:**
```csharp
// depend on the test module included in TestKit
[DependsOn(typeof(ContractTestModule))]
public class GreeterContractTestModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // disable authority for deployment
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
    }
}
```

From the previous code snippet you can note several things:
- this module is very basic and only depends on the TestKits contract test module.
- the only thing that the module does is disable some checks for contract deployment that are only needed when actually running a chain.

### Test Base

The test base is used to initialize the variables used in the test cases (such as the contract stub and the contract address, etc.) and to deploy the contract to be tested.

**GreeterContractTestBase.cs:**
```csharp
public class GreeterContractTestBase : ContractTestBase<GreeterContractTestModule>
    {
        private Address GreeterContractAddress { get; set; }
        private ACS0Container.ACS0Stub ZeroContractStub { get; set; }
        internal GreeterContractContainer.GreeterContractStub GreeterContractStub { get; set; }

        protected GreeterContractTestBase()
        {
            InitializeContracts();
        }

        private void InitializeContracts()
        {
            ZeroContractStub = GetZeroContractStub(SampleECKeyPairs.KeyPairs.First());

            GreeterContractAddress = AsyncHelper.RunSync(() =>
                ZeroContractStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(GreeterContract).Assembly.Location)),
                        Name = Name = Hash.FromString("AElf.ContractNames.GreeterContract"),
                        TransactionMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList()
                    })).Output;

            GreeterContractStub = GetGreeterContractStub(SampleECKeyPairs.KeyPairs.First());
        }

        private ACS0Container.ACS0Stub GetZeroContractStub(ECKeyPair keyPair)
        {
            return GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
        }

        private GreeterContractContainer.GreeterContractStub GetGreeterContractStub(ECKeyPair keyPair)
        {
            return GetTester<GreeterContractContainer.GreeterContractStub>(GreeterContractAddress, keyPair);
        }
    }
```

### Test files

Now for the easy part, the test class only needs to inherit from the test base. After this you can go ahead and create the test cases you need.

**GreeterContractTests.cs**
```csharp
public class GreeterContractTests : GreeterContractTestBase
{
    // declare the method as a xUnit test method
    [Fact]
    public async Task GreetTest()
    {
        // Use the contracts stub to call the 'Greet' method and get a reference to 
        // the transaction result.
        var txResult = await GreeterContractStub.Greet.SendAsync(new Empty());

        // check that the transaction was mined
        txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        // parse the result (return from the contract)
        var text = StringValue.Parser.ParseFrom(txResult.TransactionResult.ReturnValue);

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

We've seen how to add the tests for a contract. The next step is to add the contract to the deployment mechanism to actually be able to interact with the contract from external sources.