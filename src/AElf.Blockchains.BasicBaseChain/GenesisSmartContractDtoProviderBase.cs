using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.SmartContract.Application;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.BasicBaseChain;

/// <summary>
///     Provide dtos for genesis block contract deployment and initialization.
/// </summary>
public abstract class GenesisSmartContractDtoProviderBase : IGenesisSmartContractDtoProvider
{
    protected readonly IContractDeploymentListProvider ContractDeploymentListProvider;
    protected readonly IEnumerable<IContractInitializationProvider> ContractInitializationProviders;

    protected GenesisSmartContractDtoProviderBase(IContractDeploymentListProvider contractDeploymentListProvider,
        IEnumerable<IContractInitializationProvider> contractInitializationProviders)
    {
        ContractDeploymentListProvider = contractDeploymentListProvider;
        ContractInitializationProviders = contractInitializationProviders;
    }

    // TODO: Currently contract deployment logic are same for ContractTestBase, need to fix sooner or later.
    public virtual IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
    {
        var contractCodes = GetContractCodes();
        var deploymentList = ContractDeploymentListProvider.GetDeployContractNameList();
        return ContractInitializationProviders 
            .GroupBy(p => p.SystemSmartContractName)
            .Where(g => deploymentList.Contains(g.Key))
            .OrderBy(g => deploymentList.IndexOf(g.Key))
            .Select(g =>
            {
                var p = g.Last();
                var code = contractCodes[p.ContractCodeName];
                var methodList = p.GetInitializeMethodList(code);
                var genesisSmartContractDto = new GenesisSmartContractDto
                {
                    Code = code,
                    SystemSmartContractName = p.SystemSmartContractName,
                    ContractInitializationMethodCallList = new List<ContractInitializationMethodCall>()
                };
                foreach (var method in methodList) genesisSmartContractDto.AddGenesisTransactionMethodCall(method);

                return genesisSmartContractDto;
            });
    }

    protected abstract IReadOnlyDictionary<string, byte[]> GetContractCodes();
}