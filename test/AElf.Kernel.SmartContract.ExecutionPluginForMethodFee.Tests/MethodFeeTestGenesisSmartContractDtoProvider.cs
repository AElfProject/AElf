using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Deployer;
using AElf.Kernel.SmartContractInitialization;
using AElf.OS.Node.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class MethodFeeTestGenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider, ITransientDependency
    {
        private readonly IContractDeploymentListProvider _contractDeploymentListProvider;
        private readonly IEnumerable<IContractInitializationProvider> _contractInitializationProviders;

        public MethodFeeTestGenesisSmartContractDtoProvider(
            IContractDeploymentListProvider contractDeploymentListProvider,
            IEnumerable<IContractInitializationProvider> contractInitializationProviders)
        {
            _contractDeploymentListProvider = contractDeploymentListProvider;
            _contractInitializationProviders = contractInitializationProviders;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            var codes = ContractsDeployer.GetContractCodes<MethodFeeTestGenesisSmartContractDtoProvider>();

            var deploymentList = _contractDeploymentListProvider.GetDeployContractNameList();
            return _contractInitializationProviders
                .Where(p => deploymentList.Contains(p.SystemSmartContractName))
                .OrderBy(p => deploymentList.IndexOf(p.SystemSmartContractName))
                .Select(p =>
                {
                    var code = codes[p.ContractCodeName];
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