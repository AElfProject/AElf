using System.Collections.Generic;
using System.Linq;
using AElf.Blockchains.ContractInitialization;
using AElf.Contracts.Deployer;
using AElf.Kernel.SmartContract;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain
{
    /// <summary>
    /// Provide dtos for genesis block contract deployment and initialization.
    /// </summary>
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _codes;

        private readonly List<IContractInitializationProvider> _contractInitializationProviders;

        public GenesisSmartContractDtoProvider(
            IServiceContainer<IContractInitializationProvider> contractInitializationProviders,
            IOptionsSnapshot<ContractOptions> contractOptions)
        {
            _contractInitializationProviders = contractInitializationProviders.ToList();
            _codes = ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>(contractOptions.Value
                .GenesisContractDir);
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            return _contractInitializationProviders.Select(provider => provider.GetGenesisSmartContractDto(_codes))
                .ToList();
        }
    }
}