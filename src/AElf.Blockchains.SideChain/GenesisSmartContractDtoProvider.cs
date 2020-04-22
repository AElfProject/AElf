using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Deployer;
using AElf.CrossChain;
using AElf.CrossChain.Application;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractInitialization;
using AElf.Kernel.Token;
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
        private readonly IContractDeploymentListProvider _contractDeploymentListProvider;
        private readonly IServiceContainer<IContractInitializationProvider> _contractInitializationProviders;

        public ILogger<GenesisSmartContractDtoProvider> Logger { get; set; }

        public GenesisSmartContractDtoProvider(
            ISideChainInitializationDataProvider sideChainInitializationDataProvider,
            IContractDeploymentListProvider contractDeploymentListProvider,
            IServiceContainer<IContractInitializationProvider> contractInitializationProviders,
            IOptionsSnapshot<ContractOptions> contractOptions)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
            _contractDeploymentListProvider = contractDeploymentListProvider;
            _contractInitializationProviders = contractInitializationProviders;
            _codes = ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>(contractOptions.Value
                .GenesisContractDir);
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _sideChainInitializationDataProvider.GetChainInitializationDataAsync());

            if (chainInitializationData == null)
            {
                return new List<GenesisSmartContractDto>();
            }

            var deploymentList = _contractDeploymentListProvider.GetDeployContractNameList();
            return _contractInitializationProviders
                .Where(p => deploymentList.Contains(p.SystemSmartContractName))
                .OrderBy(p => deploymentList.IndexOf(p.SystemSmartContractName))
                .Select(p =>
                {
                    var code = _codes[p.ContractCodeName];
                    var methodList = p.GetInitializeMethodList(code);
                    var genesisSmartContractDto = new GenesisSmartContractDto
                    {
                        Code = code,
                        SystemSmartContractName = p.SystemSmartContractName
                    };
                    foreach (var method in methodList)
                    {
                        genesisSmartContractDto.TransactionMethodCallList.Value.Add(
                            new SystemContractDeploymentInput.Types.SystemTransactionMethodCall
                            {
                                MethodName = method.MethodName,
                                Params = method.Params
                            });
                    }

                    return genesisSmartContractDto;
                });
        }
    }
}