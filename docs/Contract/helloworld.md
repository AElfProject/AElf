This tutorial will guide you through the steps to create and deploy a very simple contract in C#.  

First, note that most of the steps involve the command line, but if you’re a C# developer and you recognize these steps you can use Visual Studio/Rider or any other IDE that supports csproj and nugget packages feel free to use them.

This tutorial assumes that you have already built the **aelf command line tool** and **node executable** (if not please follow this []).

Create a new directory and navigate into it:
```bash
mkdir test-aelf; cd test-aelf/
```

Execute the following command to create the contracts C# project:
```bash
dotnet new classlib; rm Class1.cs
```

Add reference to the sdk:
```bash
dotnet add test-aelf.csproj package AElf.Sdk.CSharp
```
Create the following files:
```bash
touch Protobuf/Proto/hello_world.proto HelloWorldContract.cs HelloWorldContractState.cs
```
Copy the following dependency:
```bash
cp ~/Programming/aelf/AELF/protobuf/aelf_options.proto Protobuf/Proto/
```

Open the folder with your favorite editor, paste the following in Hello_world.proto:

```bash
syntax = "proto3";
import "aelf_options.proto";
import "google/protobuf/empty.proto";
option csharp_namespace = "Demo.HelloWorldContract.Messages";
service HelloWorldContract {
    
    option (aelf.csharp_state) = "Demo.HelloWorldContract.HelloWorldContractState";
    
    rpc Hello (google.protobuf.Empty) returns (HelloReturn) { }
}
message HelloReturn {
    string Value = 1;
}

```
AElf contracts are implemented based on gRPC services. The previous code is the definition of the service and we will later use grpc to generate the C# code based on this definition. 

This service defines one contract method that takes no arguments and return a message of type **HelloReturn** that is define below it. This message only contains one field of type string.

Paste the following in HelloWorldContract.cs:
```bash
using Demo.HelloWorldContract.Messages;
using Google.Protobuf.WellKnownTypes;
namespace Demo.HelloWorldContract
{
    public partial class HelloWorldContract : HelloWorldContractContainer.HelloWorldContractBase   
    {
        public override HelloReturn Hello(Empty input)
        {
            return new HelloReturn { Value = "Hello world!"};
        }
    }
}

```

The behavior of a contracts method is implemented by overriding gRPCs generated methods. Here you can see that we override the generated **Hello** method to implement the behavior we need. In this case the method directly returns an **HelloReturn” message containing the “Hello world!” string.

Paste the following in HelloWorldContractState.cs:

```bash
using AElf.Sdk.CSharp.State;
namespace Demo.HelloWorldContract
{
    public class HelloWorldContractState : ContractState
    {
    }
}

```

Every contract can have a state and for this you need to define a class derived from ContractState. This will be explained more in detail in later tutorials because our HelloWorld contract doesn’t need it.

We have implemented a custom plugin to generate the service code from the proto definitions. You will need to copy it from the clonned repo to the current directory:cp path/to/aelfrepo/scripts/contract_csharp_plugin_osx .
Execute the following command to generate the C# files:
```bash
protoc --proto_path=./Protobuf/Proto --csharp_out=./Protobuf/Generated --csharp_opt=file_extension=.g.cs --contract_out=./Protobuf/Generated --plugin=protoc-gen-contract=contract_csharp_plugin_osx hello_world.proto
```

Build the project:
```bash
dotnet build test-aelf.csproj
```

Start the node

//todo
Start the CLI and execute the following command.
dotnet AElf.CLI.dll deploy 0 AElf.Contracts.xxxx -a xxx -p xxx


