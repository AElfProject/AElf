After defining a smart contract the next step is to generate the code from the proto definitions and implement the desired behavior.

### Implementation

Let's take the example from the previous section on defining the service:

```json
service TokenContract {
    option (aelf.csharp_state) = "AElf.Contracts.MultiToken.TokenContractState";

    // Actions
    rpc Create (CreateInput) returns (google.protobuf.Empty) { }
    rpc Transfer (TransferInput) returns (google.protobuf.Empty) { }

    // Views
    rpc GetBalance (GetBalanceInput) returns (GetBalanceOutput) {
        option (aelf.is_view) = true;
    }
}
```

When inputing this to our protobuf plugin it will generate the corresponding C# classes. Note that this guide will use C# as an example, but the message and service definitions can be used to generate code in many different languages. The plugin will produce a base class that is destined to be overriden by the contract writer to implement the necessary logic. 

So in our example, it will generate a base class the we can override with our own logic:

```csharp
public partial class TokenContract : TokenContractImplContainer.TokenContractImplBase
{
    public override Empty Create(CreateInput input)
    {
        var exists = State.TokenInfos[input.Symbol];

        Assert(exists == null || exists == new TokenInfo(), "Token already exists.");

        RegisterTokenInfo(...);
        
        return new Empty();
    }
}
```

The **TokenContractImplContainer.TokenContractImplBase** class is the generated class that contains unimplement methods that correspond those defined in the proto file. You'll also find that the messages that where defined are also accessible like **CreateInput**.

#### Using contract state

The following option, presented previously, defines which type the contract will use as a state:

```csharp
option (aelf.csharp_state) = "AElf.Contracts.MultiToken.TokenContractState";
```

This class needs to be created by the implementor of the smart contract and has to inherite the **ContractState** class of the C# SDK.

```csharp
public class TokenContractState : ContractState
{
    public MappedState<string, TokenInfo> TokenInfos { get; set; }
    public MappedState<Address, string, long> Balances { get; set; }
}
```

An instance of this class is accessible through the smart contracts base class with the **State** variable. In our case this will give access to the TokenInfos and Balances **MappedState**.

#### State types

More on states.

### Using the context

#### Assertions 

In the previous example, you can see the use of the Assert api:

```csharp
Assert(exists == null || exists == new TokenInfo(), "Token already exists.");
```