# What's this
Since `AElf.Contracts.TestKit` is a contract testing framework for testing AElf smart contracts, `AElf.Contract.TestKit.AEDPoSExtension` is an extension for `AElf.Contracts.TestKit` with which contract developers can test their contract in a more reality environment as AElf Main Chain.

# How to build a contract test project with AEDPoS extension
1. Create a XUnit project.

2. Modify csproj file.
Basically add bellowing references:
```MSBUILD
    <ItemGroup>
        <PackageReference Include="coverlet.msbuild" Version="2.5.1" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="16.2.0" />
        <PackageReference Include="Shouldly" Version="3.0.2" />
        <PackageReference Include="xunit" Version="2.4.1" />
        <PackageReference Include="xunit.runner.console" Version="2.4.1" />
        <PackageReference Include="xunit.runner.visualstudio" Version="2.4.1" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\..\src\AElf.Contracts.TestKit.AEDPoSExtension\AElf.Contracts.TestKit.AEDPoSExtension.csproj" />
        <ProjectReference Include="..\..\src\AElf.Kernel.Token\AElf.Kernel.Token.csproj" />
    </ItemGroup>
    <Import Project="..\AllContracts.props" />
```
Import `AllContracts.props` will actually reference all system contracts.
At the same time, you need to reference the contract you are about to test to this testing project.
Meanwhile if you want to test `AElf.Contracts.TestContract.BasicFunction`, what you need is to add bellowing lines to csproj file:
```MSBUILD
    <ItemGroup>
        <ProjectReference Include="..\AElf.Contracts.TestContract.BasicFunction\AElf.Contracts.TestContract.BasicFunction.csproj">
            <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
            <OutputItemType>Contract</OutputItemType>
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </ProjectReference>
    </ItemGroup>
```
Because this contract isn't a system contract, you need to add this reference separately.

3. Link necessary proto files for generating contract stub for testing.
In Demo project, we choose these proto files.
```MSBUILD
    <ItemGroup>
        <ContractStub Include="..\..\protobuf\token_contract.proto">
            <Link>Protobuf/Proto/token_contract.proto</Link>
        </ContractStub>

        <ContractMessage Include="..\..\protobuf\acs4.proto">
            <Link>Protobuf\Proto\acs4.proto</Link>
        </ContractMessage>
        <ContractMessage Include="..\..\protobuf\aedpos_contract.proto">
            <Link>Protobuf\Proto\aedpos_contract.proto</Link>
        </ContractMessage>
        <ContractStub Include="..\..\protobuf\aedpos_contract_impl.proto">
            <Link>Protobuf\Proto\aedpos_contract_impl.proto</Link>
        </ContractStub>

        <ContractStub Include="..\..\protobuf\test_basic_function_contract.proto">
            <Link>Protobuf\Proto\test_basic_function_contract.proto</Link>
        </ContractStub>
    </ItemGroup>
```

4. Create a `Module`
See `AEDPoSExtensionDemoModule`.
Config ContractOptions.ContractDeploymentAuthorityRequired as false is necessary currently.

5. Create a `TestBase`
Inherit from AEDPoSExtensionTestBase.
Then Deploy system smart contracts in ctor of `TestBase`.
See `AEDPoSExtensionDemoTestBase`.
Note: AEDPoS Contract will deploy anyway because AEDPoS processes is of vital importance exactly for this testing framework. 

6. For writing test cases, you can check code in this demo project.