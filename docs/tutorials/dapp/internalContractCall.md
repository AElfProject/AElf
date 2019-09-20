# Internal contract interaction

Generally, there are two reasons for us to write code to interact with other contracts (sometimes even the contract you're writing).

1. To query a state from other contracts.
2. To send a new transaction which will be executed after the original transaction.

Both of the two operations can be done in two ways:

1. Using *CSharpSmartContract.Context*.
2. Adding a *Contract Reference State*, then using *CSharpSmartContract.State*.

## Using the Context

### Query a state from other contracts

Let's see how to call the **GetCandidates** method of **Election Contract** and get return value directly in your contract code. The **Context** property is available for every smart contract.

```csharp
using AElf.Sdk.CSharp;
using AElf.Contracts.Election;

...

// your contract code needs the candidates
var electionContractAddress =
    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);

// call the method
var candidates = Context.Call<PubkeyList>(electionContractAddress, "GetCandidates", new Empty());

// use **candidates** to do other stuff...
```

There are several things to know before writing such code.

- Because this code references a type (**PubkeyList**) originally defined in the Election Contract (types are defined in a proto file, in this case  **election_contract.proto**), you at least need to reference messages defined in the .proto file in your contracts project.

Add these lines to your csproj file:
```xml
    <ItemGroup>
        <ContractMessage Include="..\..\protobuf\election_contract.proto">
            <Link>Protobuf\Proto\reference\election_contract.proto</Link>
        </ContractMessage>
    </ItemGroup>
```
The **ContractMessage** tag means you just want to reference the messages defined in the specified .proto file.

- The `Call` method take the three following parameters: 
    - *address*: the address of the contract you're seeking to interact with.
    - *methodName*: the name of method you want to call.
    - *message*: the argument for calling that method.

- Since the `Election Contract` is a system contract which deployed at the very beginning of AElf blockchain, we can get its address directly from the `Context` property. If you want to call contracts deployed by users, you may need to obtain the address in another way (like hard code).

### To send an inline transaction

Imagine you want to transfer some tokens from the contract you're writing, the necessary step is sending an inline transaction to `MultiToken Contract`, and the `MethodName` of this inline transaction needs to be `Transfer `.

```C#
var tokenContractAddress = Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
Context.SendInline(tokenContractAddress, "Transfer", new TransferInput
{
    To = /* The address you wanna transfer to*/,
    Symbol = Context.Variables.NativeSymbol,// You will get "ELF" if this contract is deployed in AElf main chain.
    Amount = 100_000_00000000,// 100000 ELF tokens.
    Memo = "Gift."// Optional
});
```

Again, because you have to reference a message defined by multi-token contract proto file, you need to add these lines to the csproj file of your contract project.

```C#
    <ItemGroup>
        <ContractMessage Include="..\..\protobuf\token_contract.proto">
            <Link>Protobuf\Proto\reference\token_contract.proto</Link>
        </ContractMessage>
    </ItemGroup>
```

This inline transaction will be executed after the execution of the original transaction.
Check other documentation for more details about the inline transactions.

## Using `Contract Reference State`

Using `Contract Reference State` is more convenient than using `Context` to do the interaction with another contract.
Follow these three steps of preparation:

1. Add related proto file(s) of the contract you want to call or send inline transactions to and rebuild the contract project. (just like before but we need to change the MSBUILD tag name, we'll see this later.)
2. Add an internal property of `XXXContractReferenceState` type to the State class of your contract.
3. Set the contract address to the `Value` of property you just added in step 2.

Let's see a demo that implement these steps: check the balance of ELF token of current contract, if the balance is greater than 100 000, request a random number from `AEDPoS Contract`.

First, reference proto files related to `MultiToken Contract` and `acs6.proto` (random number generation).
```C#
    <ItemGroup>
        <ContractReference Include="..\..\protobuf\acs6.proto">
            <Link>Protobuf\Proto\reference\acs6.proto</Link>
        </ContractReference>
        <ContractReference Include="..\..\protobuf\token_contract.proto">
            <Link>Protobuf\Proto\reference\token_contract.proto</Link>
        </ContractReference>
    </ItemGroup>
```
After rebuilding the contract project, we'll see following files appear in the Protobuf/Generated folder:
- Acs6.c.cs
- Acs6.g.cs
- TokenContract.c.cs
- TokenContract.g.cs

As you may guess, the entities we will use are defined in files above.

Here we will define two `Contract Reference States`, one for the token contract and one for the random number provider.

```C#
using AElf.Contracts.MultiToken;
using Acs6;

...

// Define these properties in the State file of current contract.
internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
internal RandomNumberProviderContractContainer.RandomNumberProviderContractReferenceState ACS6Contract { get; set }
```

Life becomes very easy if we have these `XXXContractReferenceState` instances. Check the implementation.

```C#

// Set the Contract Reference States address before using it (again here, we already have the system addresses for the token and ac6 contracts).
if (State.TokenContract.Value == null)
{
    State.TokenContract.Value =
        Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
}
if (State.ACS6Contract.Value == null)
{
    // This means we use the random number generation service provided by `AEDPoS Contract`.
    State.ACS6Contract.Value =
        Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
}

// Use `Call` method to query states from multi-token contract.
var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
{
    Owner = Context.Self,// The address of current contract.
    Symbol = "ELF"// Also, you can use Context.Variables.NativeSymbol if this contract will deployed in AElf main chain.
});
if (balance.Balance > 100_000)
{
    // Use `Send` method to generate an inline transaction.
    State.ACS6Contract.RequestRandomNumber.Send(new RequestRandomNumberInput());
}
```

As you can see it is convenient to call a method by using the state property like this: State.**Contract**.**method**.Call(**input**).