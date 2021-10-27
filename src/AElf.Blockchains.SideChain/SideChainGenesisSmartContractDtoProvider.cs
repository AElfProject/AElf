using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS0;
using AElf.Blockchains.BasicBaseChain;
using AElf.ContractDeployer;
using AElf.CrossChain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    public class SideChainGenesisSmartContractDtoProvider : GenesisSmartContractDtoProviderBase
    {
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;
        private readonly ContractOptions _contractOptions;

        public ILogger<SideChainGenesisSmartContractDtoProvider> Logger { get; set; }

        public SideChainGenesisSmartContractDtoProvider(
            ISideChainInitializationDataProvider sideChainInitializationDataProvider,
            IContractDeploymentListProvider contractDeploymentListProvider,
            IEnumerable<IContractInitializationProvider> contractInitializationProviders,
            IOptionsSnapshot<ContractOptions> contractOptions)
            :base(contractDeploymentListProvider, contractInitializationProviders)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
            _contractOptions = contractOptions.Value;
        }

        public override IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _sideChainInitializationDataProvider.GetChainInitializationDataAsync());

            if (chainInitializationData == null)
            {
                return new List<GenesisSmartContractDto>();
            }

            return base.GetGenesisSmartContractDtos();
        }

        protected override IReadOnlyDictionary<string, byte[]> GetContractCodes()
        {
            return ContractsDeployer.GetContractCodes<SideChainGenesisSmartContractDtoProvider>(_contractOptions
                .GenesisContractDir);
        }
    }
}