# ACS2 - Parallel Execution Standard

ACS2 is used to provide information for parallel execution of transactions.

## Interface

A contract that inherits ACS2 only needs to implement one method:

* GetResourceInfo

The parameter is the Transaction type, and the return value is the type ResourceInfo defined in acs2.proto:

```proto
message ResourceInfo {
    repeated aelf.ScopedStatePath paths = 1;
    bool non_parallelizable = 2;
}
```

aelf.ScopedStatePath is defined in aelf\core.proto:

```proto
message ScopedStatePath {
    Address address = 1;
    StatePath path = 2;
}
message StatePath {
    repeated string parts = 1;
}
```

## Usage

AElf uses the key-value database to store data. For the data generated during the contract execution, a mechanism called State Path is used to determine the key of the data.

For example Token contract‘s State file defines a property MappedState < Address, string, long >Balances, it can be used to access, modify balance.

Assuming that the address of the Token contract is **Nmjj7noTpMqZ522j76SDsFLhiKkThv1u3d4TxqJMD8v89tWmE**. If you want to know the balance of the address **2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz**, you can directly use this key to access redis / ssdb to get its value.

``` text
Nmjj7noTpMqZ522j76SDsFLhiKkThv1u3d4TxqJMD8v89tWmE/Balances/2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz/ELF
```

On AElf, the implementation of parallel transaction execution is also based on the key , developers need to provide a method may access to the StatePath, then the corresponding transactions will be properly grouped before executing: if the two methods do not access the same StatePath, then you can safely place them in different groups.

Attention: The transaction will be canceled and labeled to "can not be groupped" when the StatePath mismatchs the method.

If you are interested in the logic, you can view the code ITransactionGrouper, as well as IParallelTransactionExecutingService .

## Implementation

A example: within the Token contract, the core logic of method Transfer is to modify the balance of address. It accesses the balances property mentioned above twice.

At this point, we need to notify ITransactionGrouper via the GetResourceInfo method of the key of the ELF balance of address A and address B:

```c#
var args = TransferInput.Parser.ParseFrom(txn.Params);
var resourceInfo = new ResourceInfo
{
    Paths =
    {
        GetPath(nameof(TokenContractState.Balances), txn.From.ToString(), args.Symbol),
        GetPath(nameof(TokenContractState.Balances), args.To.ToString(), args.Symbol),
    }
};
return resourceInfo;
```

The GetPath forms a ScopedStatePath from several pieces of data that make up the key:

```c#
private ScopedStatePath GetPath(params string[] parts)
{
    return new ScopedStatePath
    {
        Address = Context.Self,
        Path = new StatePath
        {
            Parts =
            {
                parts
            }
        }
    }
}
```

## Test

You can construct two transactions, and the transactions are passed directly to an implementation instance of ITransactionGrouper, and the GroupAsync method is used to see if the two transactions are parallel.

We prepare two stubs that implement the ACS2 contract with different addresses to simulate the Transfer:

```c#
var keyPair1 = SampleECKeyPairs.KeyPairs[0];
var acs2DemoContractStub1 = GetACS2DemoContractStub(keyPair1);
var keyPair2 = SampleECKeyPairs.KeyPairs[1];
var acs2DemoContractStub2 = GetACS2DemoContractStub(keyPair2);
```

Then take out some services and data needed for testing from Application:

```c#
var transactionGrouper = Application.ServiceProvider.GetRequiredService<ITransactionGrouper>();
var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
var chain = await blockchainService.GetChainAsync();
```

Finally, check it via transactionGrouper:

```c#
// Situation can be parallel executed.
{
    var groupedTransactions = await transactionGrouper.GroupAsync(new ChainContext
    {
        BlockHash = chain.BestChainHash,
        BlockHeight = chain.BestChainHeight
    }, new List<Transaction>
    {
        acs2DemoContractStub1.TransferCredits.GetTransaction(new TransferCreditsInput
        {
            To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[2].PublicKey),
            Symbol = "ELF",
            Amount = 1
        }),
        acs2DemoContractStub2.TransferCredits.GetTransaction(new TransferCreditsInput
        {
            To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[3].PublicKey),
            Symbol = "ELF",
            Amount = 1
        }),
    });
    groupedTransactions.Parallelizables.Count.ShouldBe(2);
}
// Situation cannot.
{
    var groupedTransactions = await transactionGrouper.GroupAsync(new ChainContext
    {
        BlockHash = chain.BestChainHash,
        BlockHeight = chain.BestChainHeight
    }, new List<Transaction>
    {
        acs2DemoContractStub1.TransferCredits.GetTransaction(new TransferCreditsInput
        {
            To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[2].PublicKey),
            Symbol = "ELF",
            Amount = 1
        }),
        acs2DemoContractStub2.TransferCredits.GetTransaction(new TransferCreditsInput
        {
            To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[2].PublicKey),
            Symbol = "ELF",
            Amount = 1
        }),
    });
    groupedTransactions.Parallelizables.Count.ShouldBe(1);
}
```

## Example

You can refer to the implementation of the MultiToken contract for GetResourceInfo. Noting that for the ResourceInfo provided by the method Tranfer, you need to consider charging a transaction fee in addition to the two keys mentioned in this article.
