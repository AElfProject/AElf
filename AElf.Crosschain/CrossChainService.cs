using AElf.Kernel;

namespace AElf.Crosschain
{
    public class CrossChainService : ICrossChainService
    {
        public SideChainBlockInfo[] GetSideChainBlockInfo()
        {
            throw new System.NotImplementedException();
        }

        public ParentChainBlockInfo[] GetParentChainBlockInfo()
        {
            throw new System.NotImplementedException();
        }

        public bool ValidateSideChainBlockInfo(SideChainBlockInfo[] sideChainBlockInfo)
        {
            throw new System.NotImplementedException();
        }

        public bool ValidateParentChainBlockInfo(ParentChainBlockInfo[] parentChainBlockInfo)
        {
            throw new System.NotImplementedException();
        }
    }
}