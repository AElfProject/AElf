using AElf.GovernmentSystem;

namespace AElf.ContractTestBase;

public class MainChainParliamentContractInitializationDataProvider : IParliamentContractInitializationDataProvider
{
    public ParliamentContractInitializationData GetContractInitializationData()
    {
        return new ParliamentContractInitializationData();
    }
}