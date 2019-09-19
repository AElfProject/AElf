# Internal contract interaction

Generally, there are two reasons for us to write code to interact with other contracts (sometimes even the contract you're writing).

1. To query a state from other contracts.
2. To send a new transaction which will be executed after the original transaction.

Both of the two operations can be done in two ways:

1. Using *CSharpSmartContract.Context*.
2. Adding a *Contract Reference State*, then using *CSharpSmartContract.State*.

## Use `Context`

### To query a state from other contracts

Let's see how to call `GetCandidates` method of `Election Contract` and get return value directly in your contract code.

```C#
using AElf.Sdk.CSharp;
using AElf.Contracts.Election;

...

// Your contract code
var electionContractAddress =
    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
var candidates = Context.Call<PubkeyList>(electionContractAddress, "GetCandidates", new Empty());

// Use candidates to do other stuff.
...
```

There are several things to know before writing such code.

- Because you have to reference types defined in the election contract proto file (election_contract.proto), so at least you need to reference messages of that proto file to your contract project.
Add these lines to your csproj file:
```C#
    <ItemGroup>
        <ContractMessage Include="..\..\protobuf\election_contract.proto">
            <Link>Protobuf\Proto\reference\election_contract.proto</Link>
        </ContractMessage>
    </ItemGroup>
```
`ContractMessage` tag means you just want to reference the messages defined in this proto file.
- There parameters in `Call` method: 
    - *address*. The address of the contract you're seeking to interact.
    - *methodName*. The name of method you want to call.
    - *message*. The argument for calling that method.

- Since the `Election Contract` is a system contract which deployed at the very beginning of AElf blockchain, that's why we can get its address directly from `Context` property. If you want to call contracts deployed by users, you may need to obtain the address in another way (like hard code).

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

## Use `Contract Reference State`

Using `Contract Reference State` is more convenient than using `Context` to do the interaction with another contract.
Take three steps of preparing:

1. Add related proto file(s) of the contract you want to call or send inline transactions; rebuild the contract project. (Just like before but we need to change the MSBUILD tag name, we'll see this later.)
2. Add an internal property of `XXXContractReferenceState` type to the State class of your contract.
3. Set the contract address to the `Value` of property you just added in step 2.

Let's see a demo that implement this situation: check the balance of ELF token of current contract, if the balance is greater than 100000, request a random number from `AEDPoS Contract`.

First, reference proto files related to `MultiToken Contract` and `acs6.proto`.
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
After rebuilding the contract project, we'll see following files already exist in Protobuf/Generated folder:
- Acs6.c.cs
- Acs6.g.cs
- TokenContract.c.cs
- TokenContract.g.cs

As you may guess, the entities we will use are defined in files above.

Then we can define two `Contract Reference State`s.

```C#
using AElf.Contracts.MultiToken;
using Acs6;

...

// Define these properties in the State file of current contract.
internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
internal RandomNumberProviderContractContainer.RandomNumberProviderContractReferenceState ACS6Contract
{
    get;
    set;
}
```

Life becomes very easy if we have these `XXXContractReferenceState` instances. Check the implementation.

```C#

// Set address to Contract Reference States.
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

// Logic.
// Use `Call` method to query states from multi-token contract.
var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
{
    Owner = Context.Self,// The address of current contract.
    Symbol = "ELF"// Also, you can use Context.Variables.NativeSymbol if this contract will deployed in AElf main chain.
});
if (balance.Balance > 1_000_000_00000000)
{
    // Use `Send` method to generate an inline transaction.
    State.ACS6Contract.RequestRandomNumber.Send(new RequestRandomNumberInput());
}

```