using AElf.Runtime.WebAssembly;
using AElf.Sdk.CSharp;
using AElf.SolidityContract;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis;

public partial class BasicContractZero
{
    public override Address DeploySoliditySmartContract(DeploySoliditySmartContractInput input)
    {
        var category = input.Category;
        var wasmCode = new WasmContractCode();
        wasmCode.MergeFrom(input.Code);
        var constructorInput = input.Parameter;
        var author = Context.Sender;
        var codeHash = wasmCode.CodeHash;
        var serialNumber = State.ContractSerialNumber.Value;
        State.ContractSerialNumber.Value = serialNumber + 1;

        var contractAddress = AddressHelper.ComputeContractAddress(Context.ChainId, serialNumber);

        var info = new ContractInfo
        {
            SerialNumber = serialNumber,
            Author = author,
            Category = category,
            CodeHash = codeHash,
            Version = 1,
            IsUserContract = true
        };

        var reg = new SmartContractRegistration
        {
            Category = input.Category,
            Code = wasmCode.ToByteString(),
            CodeHash = codeHash,
            IsSystemContract = info.IsSystemContract,
            Version = info.Version,
            ContractAddress = contractAddress,
            IsUserContract = true
        };

        var contractInfo = Context.DeploySmartContract(contractAddress, reg, null);

        info.ContractVersion = contractInfo.ContractVersion;
        reg.ContractVersion = info.ContractVersion;

        State.ContractInfos[contractAddress] = info;
        State.SmartContractRegistrations[reg.CodeHash] = reg;

        // Duplicate contract info for delegate calling.
        var codeHashAddress = Address.FromBytes(codeHash.ToByteArray());
        State.ContractInfos[codeHashAddress] = info;

        Context.ExecuteContractConstructor(contractAddress, reg, author, constructorInput ?? ByteString.Empty);

        Context.Fire(new ContractDeployed
        {
            CodeHash = codeHash,
            Address = contractAddress,
            Author = author,
            Version = info.Version,
            ContractVersion = info.ContractVersion
        });

        return contractAddress;
    }

    public override Address InstantiateSoliditySmartContract(InstantiateSoliditySmartContractInput input)
    {
        var category = input.Category;
        var codeHash = input.CodeHash;
        var reg = State.SmartContractRegistrations[codeHash];
        if (reg == null)
        {
            throw new AssertionException($"Contract code of hash {input.CodeHash} not found.");
        }

        Assert(reg.Category == category, "Category not match.");
        var author = Context.Sender;
        var serialNumber = State.ContractSerialNumber.Value;
        State.ContractSerialNumber.Value = serialNumber + 1;

        var contractAddress = AddressHelper.ComputeContractAddress(Context.ChainId, serialNumber);

        var info = new ContractInfo
        {
            SerialNumber = serialNumber,
            Author = author,
            Category = category,
            CodeHash = codeHash,
            Version = 1,
            IsUserContract = true
        };

        var contractInfo = Context.DeploySmartContract(contractAddress, reg, null);

        info.ContractVersion = contractInfo.ContractVersion;

        State.ContractInfos[contractAddress] = info;

        // Duplicate contract info for delegate calling.
        var codeHashAddress = Address.FromBytes(codeHash.ToByteArray());
        State.ContractInfos[codeHashAddress] = info;

        Context.Fire(new ContractDeployed
        {
            CodeHash = codeHash,
            Address = contractAddress,
            Author = author,
            Version = info.Version,
            ContractVersion = info.ContractVersion
        });

        var contractCodeHashList =
            State.ContractCodeHashListMap[Context.CurrentHeight] ?? new ContractCodeHashList();
        contractCodeHashList.Value.Add(codeHash);
        State.ContractCodeHashListMap[Context.CurrentHeight] = contractCodeHashList;

        return contractAddress;
    }

    public override Hash UploadSoliditySmartContract(UploadSoliditySmartContractInput input)
    {
        var wasmCode = new WasmContractCode();
        wasmCode.MergeFrom(input.Code);
        var codeHash = wasmCode.CodeHash;
        AssertContractExists(codeHash);
        var reg = new SmartContractRegistration
        {
            Category = input.Category,
            Code = input.Code,
            CodeHash = codeHash,
            ContractAddress = Address.FromBytes(codeHash.ToByteArray()),
            IsUserContract = true
        };
        State.SmartContractRegistrations[codeHash] = reg;
        return codeHash;
    }

    public override Empty UpdateSoliditySmartContract(UpdateSoliditySmartContractInput input)
    {
        var contractInfo = State.ContractInfos[input.ContractAddress];
        if (contractInfo == null)
        {
            throw new AssertionException("Contract info not found.");
        }

        contractInfo.CodeHash = input.CodeHash;
        State.ContractInfos[input.ContractAddress] = contractInfo;
        return new Empty();
    }
}