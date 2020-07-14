using System.Collections.Generic;
using AElf.Blockchains.BasicBaseChain;
using AElf.ContractDeployer;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.PoWChain
{
    public class PoWChainGenesisSmartContractDtoProvider : GenesisSmartContractDtoProviderBase
    {
        private readonly ContractOptions _contractOptions;

        public PoWChainGenesisSmartContractDtoProvider(IContractDeploymentListProvider contractDeploymentListProvider,
            IEnumerable<IContractInitializationProvider> contractInitializationProviders,
            IOptionsSnapshot<ContractOptions> contractOptions)
            : base(contractDeploymentListProvider, contractInitializationProviders)
        {
            _contractOptions = contractOptions.Value;
        }

        protected override IReadOnlyDictionary<string, byte[]> GetContractCodes()
        {
            return ContractsDeployer.GetContractCodes<PoWChainGenesisSmartContractDtoProvider>(_contractOptions
                .GenesisContractDir);
        }
    }
}