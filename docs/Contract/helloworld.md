This tutorial will guide you through the steps to create and deploy a simple contract in C#.  

First, note that most of the steps involve the command line, but if you’re a C# developer and you recognize these steps you can use Visual Studio/Rider or any other IDE that supports csproj and nugget packages, feel free to use them.

This tutorial assumes that you have already built the **aelf command line tool** and **node executable** (if not please follow [this](../Introduction/quickstart.md) guide).

Open a terminal and create a new directory and navigate into it:
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
Execute the following command to create the files we need to implement the contract:
```bash
mkdir -p Protobuf/Proto;touch Protobuf/Proto/hello_world.proto HelloWorldContract.cs HelloWorldContractState.cs
```
Copy the following dependencies from the **protobuf** folder located at the root of your aelf folder:
```bash
cp ~/path/to/aelf/protobuf/aelf_options.proto Protobuf/Proto/
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

We have implemented a custom plugin to generate the service code from the proto definitions. You will need to copy it from the clonned repo to the current directory:

```bash
cp path/to/aelfrepo/scripts/contract_csharp_plugin .
```

Create the folder for the generated files:
```bash
mkdir ./Protobuf/Generated
```

Execute the following command to generate the C# files:
```bash
protoc --proto_path=./Protobuf/Proto --csharp_out=./Protobuf/Generated --csharp_opt=file_extension=.g.cs --contract_out=./Protobuf/Generated --plugin=protoc-gen-contract=contract_csharp_plugin hello_world.proto
```

This example also has a dependency on Protobuf so we need to add the depency:
```bash
dotnet add package Google.Protobuf
```

Build the project:
```bash
dotnet build test-aelf.csproj
```

Open another terminal and navigate to AElfs root directory then enter **AElf.Launcher** to run the node:

```bash
cd AElf.Launcher/
dotnet bin/Release/netcoreapp2.2/AElf.Launcher.dll
```

In the previous terminal, alias the cli:

```bash
alias aelf-cli="dotnet path/to/aelf/AElf.CLI/bin/Release/netcoreapp2.2/AElf.CLI.dll"
```

Start the CLI and execute the following command, note that here we explicitly specify the datadir (the directory where you keep your keys).

```bash
aelf-cli deploy 0 bin/Debug/netstandard2.0/test-aelf.dll -a 4Mjy1siZA5TBkky2FLsDQ93QcSi3DuySjd1AzVfpjTHNBuc -e http://127.0.0.1:1728 -d path/to/datadir
```

The specified account will be the owner of the contract, he will be the only one that can update the code of the contract and he also has the options of changing the owner of the contract.

The return of the deploy command will give you the transaction ID that you can use to retrieve its status:
```bash
TransactionId is: ab435790a62abd6a669d002d56771b27bb683a73ce46de0f389ec045e4f3405c
```

Verify the result:
```bash
aelf-cli get-tx-result ab435790a62abd6a669d002d56771b27bb683a73ce46de0f389ec045e4f3405c -e http://127.0.0.1:1728 
```

In the result you should see the following: 
```bash 
"TransactionId": "ab435790a62abd6a669d002d56771b27bb683a73ce46de0f389ec045e4f3405c",
"Status": "Mined",
"Logs": [
"ReadableReturnValue": "\"4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6\"",
],
```

The status of this deployement should be "Mined" and with this result you will be able to call the contract by its address: "4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6". The following command will call the contract with empty parameter ('{}').

```bash
aelf-cli call 4QjhKLWacRXrQYpT7rzf74k5XZFCx8yF3X7FXbzKD4wwEo6 Hello '{}' -a 4Mjy1siZA5TBkky2FLsDQ93QcSi3DuySjd1AzVfpjTHNBuc -p password -e http://127.0.0.1:1728
```

The **-p** option here specifies the key to the specified account, it's necessary as this call will create a transaction. The command should return the following:

```bash
{
    Value: "Hello world!"
}
```

This is all for this introduction to smart contracts, more complete examples will be comming soon.








