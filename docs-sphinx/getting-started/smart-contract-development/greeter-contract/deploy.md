# Deploying the contract


## Add a reference

In the **AElf.Boilerplate.Mainchain** you need to reference the contract implementation project and the corresponding Proto file:


**AElf.Boilerplate.Mainchain.csproj:**
```xml
<ProjectReference Include="..\..\contract\AElf.Contracts.GreeterContract\AElf.Contracts.GreeterContract.csproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    <OutputItemType>Contract</OutputItemType>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</ProjectReference>
...
<ContractStub Include="..\..\protobuf\greeter_contract.proto">
    <Link>Protobuf\Proto\greeter_contract.proto</Link>
</ContractStub>

```

**AElf.Boilerplate.Launcher.csproj:**
```xml
<ProjectReference Include="..\..\contract\AElf.Contracts.GreeterContract\AElf.Contracts.GreeterContract.csproj">
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
    <OutputItemType>Contract</OutputItemType>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</ProjectReference>
```

## Add the DTO provider


Create a partial class for adding the genesis smart contract.

GenesisSmartContractDtoProvider_Greeter.cs
```csharp
public partial class GenesisSmartContractDtoProvider
{
    public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForGreeter()
    {
        var dto = new List<GenesisSmartContractDto>();
        dto.AddGenesisSmartContract(
            _codes.Single(kv => kv.Key.Contains("Greeter")).Value,
            Hash.FromString("AElf.ContractNames.Greeter"), new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList());
        return dto;
    }
}
```

GenesisSmartContractDtoProvider.cs
```csharp
public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
{
    // ...

    public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
    {
        // The order matters !!!
        return new[]
        {
            GetGenesisSmartContractDtosForVote(zeroContractAddress),
            GetGenesisSmartContractDtosForProfit(zeroContractAddress),
            GetGenesisSmartContractDtosForElection(zeroContractAddress),
            GetGenesisSmartContractDtosForToken(zeroContractAddress),
            // ...
            // Add the call to the previously defined method
            GetGenesisSmartContractDtosForGreeter() 
        }.SelectMany(x => x);
    }
}
```

After adding these elements, Boilerplate will deploy your contract when the node starts. You can call the Boilerplate node API:

```bash
aelf-command get-chain-status
? Enter the the URI of an AElf node: http://127.0.0.1:1235
✔ Succeed
{
  "ChainId": "AELF",
  "Branches": {
    "6032b553ec9a5c81713cf8410f426dfc1ca0f43e64d56f527fc7a9c60b90e694": 3073
  },
  "NotLinkedBlocks": {},
  "LongestChainHeight": 3073,
  "LongestChainHash": "6032b553ec9a5c81713cf8410f426dfc1ca0f43e64d56f527fc7a9c60b90e694",
  "GenesisBlockHash": "c3bddca1909ebf37b95be7f26b990e07916790913e0f48da1a831b3c777d59ff",
  "GenesisContractAddress": "2gaQh4uxg6tzyH1ADLoDxvHA14FMpzEiMqsQ6sDG5iHT8cmjp8",
  "LastIrreversibleBlockHash": "85fee024d156de3be665c296c567423026e0e3369ad7dc5ee81dbb2a15dfe2f2",
  "LastIrreversibleBlockHeight": 3042,
  "BestChainHash": "6032b553ec9a5c81713cf8410f426dfc1ca0f43e64d56f527fc7a9c60b90e694",
  "BestChainHeight": 3073
}
```

This enables further testing of the contract, including testing it from a dApp.

## Next

We've just seen through this and the previous articles how to use Boilerplate in order to develop and test a smart contract. That said, these articles only show a subset of the possibilities. 

The next article will demonstrate how to build a small front-end for the greeter contract.

