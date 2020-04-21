namespace AElf.CrossChain
{
    /// <summary>
    /// Add this interface because the initialization logic of Cross Chain Contract
    /// are different from Main Chain, Side Chain and test cases.
    /// </summary>
    public interface ICrossChainContractInitializationDataProvider
    {
        CrossChainContractInitializationData GetContractInitializationData();
    }

    public class CrossChainContractInitializationData
    {
        public int ParentChainId { get; set; }
        public long CreationHeightOnParentChain { get; set; }
        public bool IsPrivilegePreserved { get; set; }
    }
}