# Smart Contract Hello World

This tutorial will guide you through the steps to create and deploy a simple contract in C# with AElf's Boilerplate project. It will be a simple Hello World contract to demonstrate how to use our framework to create and test a contract. 

By looking at the folder structure you can see that there's the **Contract** and **Tests** folders. The contract folder is where you create the contract you are building and place the tests in the tests folder.

Both the projects should be included in the solution.

First lets look at the contract:

```bash
syntax = "proto3";

import "aelf_options.proto";
import "google/protobuf/empty.proto";

option csharp_namespace = "HelloWorldContract";

service HelloWorldContract {

    option (aelf.csharp_state) = "HelloWorldContractState";

    rpc Hello (google.protobuf.Empty) returns (HelloReturn) { }
}

message HelloReturn {
    string Value = 1;
}
```

It's a simple contract that defines one method and one type. It also needs a state:

```bash
using AElf.Sdk.CSharp.State;
namespace HelloWorldContract
{
    public class HelloWorldContractState : ContractState
    {
    }
}
```

Now lets look at the test:

```bash
public class HelloWorldContractTest : HelloWorldContractTestBase
{
    [Fact]
    public async Task Test()
    {
        var result = await HelloWorldContractStub.Hello.CallAsync(new Empty());
        result.Value.ShouldBe("Hello world!");
    }
}
```

This way you can test your contract without running the node and test your scenarios programmaticaly. This will you most of AElfs internals so it's a very complete test. 
Here the logic is simple, but it gives you the idea. On your contract stub you can call any method on the contract.

Define a module:

```bash 
public override void ConfigureServices(ServiceConfigurationContext context)
{
    base.ConfigureServices(context);
                
    Configure<RunnerOptions>(o => { o.SdkDir = Path.GetDirectoryName(typeof(HelloWorldContractTestModule).Assembly.Location); });
    context.Services.AddSingleton<IRefBlockInfoProvider, RefBlockInfoProvider>();
}
```

and the test base: 

```bash 
public class HelloWorldContractTestBase : ContractTestBase<HelloWorldContractTestModule>
{
    internal HelloWorldContractContainer.HelloWorldContractStub HelloWorldContractStub { get; set; }
    internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }

    protected Address HelloWorldContractAddress { get; set; }

    public HelloWorldContractTestBase()
    {

        var bb = ByteString.CopyFrom(File.ReadAllBytes(typeof(HelloWorldContract).Assembly.Location));
        
        BasicContractZeroStub =
            GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress,
                SampleECKeyPairs.KeyPairs[0]);
        
        HelloWorldContractAddress = AsyncHelper.RunSync(() =>
            BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(HelloWorldContract).Assembly.Location)),
                    Name = Hash.FromString("HelloWorldContract"),
                    TransactionMethodCallList = GenerateTransactionMethodCallList()
                })).Output;
        HelloWorldContractStub =
            GetTester<HelloWorldContractContainer.HelloWorldContractStub>(HelloWorldContractAddress,
                SampleECKeyPairs.KeyPairs[0]);
    }

    private SystemTransactionMethodCallList GenerateTransactionMethodCallList()
    {
        var callList = new SystemTransactionMethodCallList();
        callList.Add(nameof(HelloWorldContract.Hello), new Empty());
        return callList;
    }
}
```

That's more or less everything. We recommend you write the contract in a test driven way, when you are ready you can run the launcher project and you will be able to call the contract through the RPC to start integration testing with some external client (or the CLI if you don't plan on building some sort of dApp).