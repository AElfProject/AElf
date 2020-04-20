using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Deployer;
using AElf.ContractsInitialization;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using AElf.OS;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain
{
    /// <summary>
    /// Provide dtos for genesis block contract deployment and initialization.
    /// </summary>
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IServiceContainer<IContractInitializationProvider> _contractInitializationProviders;
        private readonly IReadOnlyDictionary<string, byte[]> _codes;

        public GenesisSmartContractDtoProvider(
            IServiceContainer<IContractInitializationProvider> contractInitializationProviders,
            IOptionsSnapshot<ContractOptions> contractOptions)

        {
            _contractInitializationProviders = contractInitializationProviders;
            _codes = ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>(contractOptions.Value
                .GenesisContractDir);
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            return _contractInitializationProviders.OrderBy(provider => provider.Tier).Select(provider =>
                provider.GetGenesisSmartContractDto(GetContractCodeByName(provider.ContractCodeName)));
        }

        private byte[] GetContractCodeByName(string name)
        {
            return _codes.Single(kv => kv.Key.Contains(name)).Value;
        }
    }
}