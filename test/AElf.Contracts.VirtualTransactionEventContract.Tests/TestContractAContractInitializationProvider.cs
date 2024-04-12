using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.TestContract.VirtualTransactionEvent;

public class TestContractAContractInitializationProvider: IContractInitializationProvider, ITransientDependency
{
    public Hash SystemSmartContractName { get; } = HashHelper.ComputeFrom("AElf.TestContractNames.A");
    public string ContractCodeName { get; } = "AElf.Contracts.TestContract.A";

    public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return new List<ContractInitializationMethodCall>();
    }
}