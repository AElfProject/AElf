using AElf.CrossChain;

namespace AElf.ContractTestBase;

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