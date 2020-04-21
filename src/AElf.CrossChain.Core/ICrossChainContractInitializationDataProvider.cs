namespace AElf.CrossChain
{
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