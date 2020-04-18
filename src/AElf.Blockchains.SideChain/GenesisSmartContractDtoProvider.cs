using System.Collections.Generic;
using System.Linq;
using AElf.Blockchains.ContractInitialization;
using AElf.Contracts.Deployer;
using AElf.Kernel.SmartContract;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _codes;

        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;
        private readonly List<IContractInitializationProvider> _contractInitializationProviders;

        public ILogger<GenesisSmartContractDtoProvider> Logger { get; set; }

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ContractOptions> contractOptions,
            ISideChainInitializationDataProvider sideChainInitializationDataProvider,
            IServiceContainer<IContractInitializationProvider> contractInitializationProviders)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
            _contractInitializationProviders = contractInitializationProviders.ToList();
            _codes = ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>(contractOptions.Value
                .GenesisContractDir);
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            var genesisSmartContractDtoList = new List<GenesisSmartContractDto>();

            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _sideChainInitializationDataProvider.GetChainInitializationDataAsync());

            if (chainInitializationData == null)
            {
                return genesisSmartContractDtoList;
            }

            genesisSmartContractDtoList.AddRange(
                _contractInitializationProviders.Select(provider => provider.GetGenesisSmartContractDto(_codes)));

            return genesisSmartContractDtoList;
        }
    }
}