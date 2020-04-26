using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Kernel.SmartContractInitialization;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.BasicBaseChain
{
    /// <summary>
    /// Provide dtos for genesis block contract deployment and initialization.
    /// </summary>
    public abstract class GenesisSmartContractDtoProviderBase : IGenesisSmartContractDtoProvider
    {
        protected readonly IContractDeploymentListProvider ContractDeploymentListProvider;
        protected readonly IServiceContainer<IContractInitializationProvider> ContractInitializationProviders;

        protected GenesisSmartContractDtoProviderBase(IContractDeploymentListProvider contractDeploymentListProvider,
            IServiceContainer<IContractInitializationProvider> contractInitializationProviders)
        {
            ContractDeploymentListProvider = contractDeploymentListProvider;
            ContractInitializationProviders = contractInitializationProviders;
        }

        // TODO: Currently contract deployment logic are same for ContractTestBase, need to fix sooner or later.
        public virtual IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos()
        {
            var contractCodes = GetContractCodes();
            var deploymentList = ContractDeploymentListProvider.GetDeployContractNameList();
            return ContractInitializationProviders
                .Where(p => deploymentList.Contains(p.SystemSmartContractName))
                .OrderBy(p => deploymentList.IndexOf(p.SystemSmartContractName))
                .Select(p =>
                {
                    var code = contractCodes[p.ContractCodeName];
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
        
        protected abstract IReadOnlyDictionary<string, byte[]> GetContractCodes();
    }
}