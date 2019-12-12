# Deploying the contract


## Add reference

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


Create the partial class for adding the genesis smart contract.

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

After adding these elements, Boilerplate will deploy your contract when the node starts.
