using AElf.ContractTestBase.ContractTestKit;
using AElf.GovernmentSystem;

namespace AElf.Contracts.MultiToken
{
    public class ParliamentContractInitializationDataProvider : IParliamentContractInitializationDataProvider
    {
        public ParliamentContractInitializationData GetContractInitializationData()
        {
            return new ParliamentContractInitializationData
            {
                PrivilegedProposer = ContractTestConstants.DefaultAccount.Address,
                ProposerAuthorityRequired = true
            };
        }
    }
}