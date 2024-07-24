using System.Collections.Generic;
using System.Linq;
using AElf.Blockchains.BasicBaseChain;
using AElf.ContractDeployer;
using AElf.Kernel.Plugin.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain;

/// <summary>
///     Provide dtos for genesis block contract deployment and initialization.
/// </summary>
public class MainChainGenesisSmartContractDtoProvider : GenesisSmartContractDtoProviderBase
{
    private readonly IEnumerable<IPluginContractProvider> _pluginContractProviders;
    private readonly ContractOptions _contractOptions;

    public MainChainGenesisSmartContractDtoProvider(IContractDeploymentListProvider contractDeploymentListProvider,
        IEnumerable<IContractInitializationProvider> contractInitializationProviders,
        IEnumerable<IPluginContractProvider> pluginContractProviders,
        IOptionsSnapshot<ContractOptions> contractOptions)
        : base(contractDeploymentListProvider, contractInitializationProviders)
    {
        _pluginContractProviders = pluginContractProviders;
        _contractOptions = contractOptions.Value;
    }

    protected override IReadOnlyDictionary<string, byte[]> GetContractCodes()
    {
        return ContractsDeployer.GetContractCodes<MainChainGenesisSmartContractDtoProvider>(
            _contractOptions.GenesisContractDir,
            pluginContractNames: _pluginContractProviders.Select(p => p.GetContractName()).ToList());
    }
}