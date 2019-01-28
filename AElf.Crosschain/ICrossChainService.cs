using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainService
    {
        SideChainBlockInfo[] GetSideChainBlockInfo();
        ParentChainBlockInfo[] GetParentChainBlockInfo();
        bool ValidateSideChainBlockInfo(SideChainBlockInfo[] sideChainBlockInfo);
        bool ValidateParentChainBlockInfo(ParentChainBlockInfo[] parentChainBlockInfo);
    }
}