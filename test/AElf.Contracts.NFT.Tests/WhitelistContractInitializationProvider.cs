using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.NFT;

public class WhitelistContractInitializationProvider : IContractInitializationProvider, ITransientDependency
{
    public Hash SystemSmartContractName { get; } = HashHelper.ComputeFrom("AElf.ContractNames.Whitelist");
    public string ContractCodeName => "AElf.Contracts.Whitelist";

    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>();
    }
}