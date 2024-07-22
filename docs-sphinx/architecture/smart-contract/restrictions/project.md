# Contract Project Requirements

## Project Properties
- It is required to add `ContractCode` property in your contract project, so that the contract's DLL will be post processed by AElf's contract patcher to perform necessary injections that are required by code checks during deployment. Otherwise, deployment will fail.

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <RootNamespace>AElf.Contracts.MyContract</RootNamespace>
  <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
</PropertyGroup>

<PropertyGroup>
	<ContractCode Include="..\..\protobuf\my_contract.proto">
            <Link>Protobuf\Proto\my_contract.proto</Link>
        </ContractCode>
</PropertyGroup>
```

- It is required to enable `CheckForOverflowUnderflow` for both Release and Debug mode so that your contract will use arithmetic operators that will throw `OverflowException` if there is any overflow. This is to ensure that execution will not continue in case of an overflow in your contract and result with unpredictable output.

```xml
<PropertyGroup Condition=" '$(Configuration)' == 'Debug' ">
  <CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>
</PropertyGroup>

<PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
  <CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>
</PropertyGroup>
```

If your contract contains any unchecked arithmetic operators, deployment will fail.
