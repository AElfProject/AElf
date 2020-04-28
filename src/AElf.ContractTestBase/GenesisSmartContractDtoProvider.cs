using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Kernel.SmartContractInitialization;
using AElf.OS.Node.Application;

namespace AElf.ContractTestBase
{
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IContractDeploymentListProvider _contractDeploymentListProvider;
        private readonly IServiceContainer<IContractInitializationProvider> _contractInitializationProviders;
        private readonly IContractCodeProvider _contractCodeProvider;

        public GenesisSmartContractDtoProvider(IContractDeploymentListProvider contractDeploymentListProvider,
            IServiceContainer<IContractInitializationProvider> contractInitializationProviders,
            IContractCodeProvider contractCodeProvider)
        {
            _contractDeploymentListProvider = contractDeploymentListProvider;
            _contractInitializationProviders = contractInitializationProviders;
            _contractCodeProvider = contractCodeProvider;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            var contractCode = _contractCodeProvider.Codes;
            var deploymentList = _contractDeploymentListProvider.GetDeployContractNameList();
            return _contractInitializationProviders
                .Where(p => deploymentList.Contains(p.SystemSmartContractName))
                .OrderBy(p => deploymentList.IndexOf(p.SystemSmartContractName))
                .Select(p =>
                {
                    var code = contractCode[p.ContractCodeName];
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