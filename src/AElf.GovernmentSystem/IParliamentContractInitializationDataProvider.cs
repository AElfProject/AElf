using AElf.Types;

namespace AElf.GovernmentSystem
{
    public interface IParliamentContractInitializationDataProvider
    {
        ParliamentContractInitializationData GetContractInitializationData();
    }

    public class ParliamentContractInitializationData
    {
        public Address PrivilegedProposer { get; set; }
        public bool ProposerAuthorityRequired { get; set; }
    }
}