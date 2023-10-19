# Contract Project Requirements

## Project Properties
- It is necessary to add a contract proto file to the contract directory of your contract project. This step ensures that the contract's DLL will undergo post-processing by AElf's contract patcher, enabling it to perform the necessary injections required for code checks during deployment. Failure to do so will result in a deployment failure

```
src
├── Protobuf
│   └── contract
│       └── hello_world_contract.proto
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