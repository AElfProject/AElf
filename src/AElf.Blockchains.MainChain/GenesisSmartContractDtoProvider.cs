using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Deployer;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractInitialization;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain
{
    /// <summary>
    /// Provide dtos for genesis block contract deployment and initialization.
    /// </summary>
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IContractDeploymentListProvider _contractDeploymentListProvider;
        private readonly IServiceContainer<IContractInitializationProvider> _contractInitializationProviders;
        private readonly IReadOnlyDictionary<string, byte[]> _codes;

        public GenesisSmartContractDtoProvider(IContractDeploymentListProvider contractDeploymentListProvider,
            IServiceContainer<IContractInitializationProvider> contractInitializationProviders,
            IOptionsSnapshot<ContractOptions> contractOptions)
        {
            _contractDeploymentListProvider = contractDeploymentListProvider;
            _contractInitializationProviders = contractInitializationProviders;
            _codes = ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>(contractOptions.Value
                .GenesisContractDir);
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            var deploymentList = _contractDeploymentListProvider.GetDeployContractNameList();
            return _contractInitializationProviders
                .Where(p => deploymentList.Contains(p.SystemSmartContractName))
                .OrderBy(p => deploymentList.IndexOf(p.SystemSmartContractName))
                .Select(p =>
                {
                    var code = _codes[p.ContractCodeName];
                    var methodMap = p.GetInitializeMethodMap(code);
                    var genesisSmartContractDto = new GenesisSmartContractDto
                    {
                        Code = code,
                        SystemSmartContractName = p.SystemSmartContractName
                    };
                    foreach (var (methodName, parameter) in methodMap)
                    {
                        genesisSmartContractDto.TransactionMethodCallList.Value.Add(
                            new SystemContractDeploymentInput.Types.SystemTransactionMethodCall
                            {
                                MethodName = methodName,
                                Params = parameter
                            });
                    }

                    return genesisSmartContractDto;
                });
        }
    }
}