using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.TestContract.VirtualTransactionEvent;

public class VirtualTransactionContractInitializationProvider : IContractInitializationProvider, ITransientDependency
{
    public Hash SystemSmartContractName { get; } = HashHelper.ComputeFrom("AElf.TestContractNames.VirtualTransactionEvent");
    public string ContractCodeName { get; } = "AElf.Contracts.TestContract.VirtualTransactionEvent";

    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>();
    }
}