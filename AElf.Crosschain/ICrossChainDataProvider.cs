using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainDataProvider
    {
        bool GetSideChainBlockInfo(ref SideChainBlockInfo[] sideChainBlockInfo);
        bool GetParentChainBlockInfo(ref ParentChainBlockInfo[] parentChainBlockInfo);
    }
}