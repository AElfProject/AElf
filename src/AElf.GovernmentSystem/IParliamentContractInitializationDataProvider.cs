using AElf.Types;

namespace AElf.GovernmentSystem
{
    /// <summary>
    /// Add this interface because the initialization logic of Parliament Contract
    /// are different from Main Chain, Side Chain and test cases.
    /// </summary>
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