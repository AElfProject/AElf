using AElf.GovernmentSystem;
using Volo.Abp.DependencyInjection;

namespace AElf.ContractTestBase
{
    public class MainChainParliamentContractInitializationDataProvider : IParliamentContractInitializationDataProvider
    {
        public ParliamentContractInitializationData GetContractInitializationData()
        {
            return new ParliamentContractInitializationData();
        }
    }
}