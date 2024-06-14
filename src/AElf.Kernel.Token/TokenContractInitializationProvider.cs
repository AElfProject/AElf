using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Token;

public class TokenContractInitializationProvider : IContractInitializationProvider, ITransientDependency
{
    public Hash SystemSmartContractName { get; } = TokenSmartContractAddressNameProvider.Name;
    public string ContractCodeName { get; } = "AElf.Contracts.Token";

    public TokenContractInitializationProvider(
        ITokenContractInitializationDataProvider tokenContractInitializationDataProvider)
    {
    }

    public virtual List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
    {
        return [];
    }
}