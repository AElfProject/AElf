using AElf.CrossChain;
using Volo.Abp.DependencyInjection;

namespace AElf.ContractTestBase
{
    public class MainChainCrossChainContractInitializationDataProvider : ICrossChainContractInitializationDataProvider
    {
        public CrossChainContractInitializationData GetContractInitializationData()
        {
            return new CrossChainContractInitializationData
            {
                IsPrivilegePreserved = true
            };
        }
    }
}