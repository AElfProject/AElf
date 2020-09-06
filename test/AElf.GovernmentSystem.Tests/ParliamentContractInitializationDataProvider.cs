namespace AElf.GovernmentSystem.Tests
{
    public class ParliamentContractInitializationDataProvider : IParliamentContractInitializationDataProvider
    {
        public ParliamentContractInitializationData GetContractInitializationData()
        {
            return new ParliamentContractInitializationData();
        }
    }
}