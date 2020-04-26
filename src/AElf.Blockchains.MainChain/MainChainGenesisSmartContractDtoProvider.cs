using System.Collections.Generic;
using AElf.Blockchains.BasicBaseChain;
using AElf.Contracts.Deployer;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractInitialization;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain
{
    /// <summary>
    /// Provide dtos for genesis block contract deployment and initialization.
    /// </summary>
    public class MainChainGenesisSmartContractDtoProvider : GenesisSmartContractDtoProviderBase
    {
        private readonly ContractOptions _contractOptions;

        public MainChainGenesisSmartContractDtoProvider(IContractDeploymentListProvider contractDeploymentListProvider,
            IServiceContainer<IContractInitializationProvider> contractInitializationProviders,
            IOptionsSnapshot<ContractOptions> contractOptions)
            : base(contractDeploymentListProvider, contractInitializationProviders)
        {
            _contractOptions = contractOptions.Value;
        }

        protected override IReadOnlyDictionary<string, byte[]> GetContractCodes()
        {
            return ContractsDeployer.GetContractCodes<MainChainGenesisSmartContractDtoProvider>(_contractOptions
                .GenesisContractDir);
        }
    }
}