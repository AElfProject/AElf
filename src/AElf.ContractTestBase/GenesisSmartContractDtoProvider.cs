using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS0;
using AElf.Kernel.SmartContract.Application;
using AElf.OS.Node.Application;

namespace AElf.ContractTestBase
{
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IContractDeploymentListProvider _contractDeploymentListProvider;
        private readonly IEnumerable<IContractInitializationProvider> _contractInitializationProviders;
        private readonly IContractCodeProvider _contractCodeProvider;

        public GenesisSmartContractDtoProvider(IContractDeploymentListProvider contractDeploymentListProvider,
            IEnumerable<IContractInitializationProvider> contractInitializationProviders,
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
                        SystemSmartContractName = p.SystemSmartContractName,
                        ContractInitializationMethodCallList = new List<ContractInitializationMethodCall>()
                    };
                    foreach (var method in methodList)
                    {
                        genesisSmartContractDto.AddGenesisTransactionMethodCall(method);
                    }

                    return genesisSmartContractDto;
                });
        }
    }
}