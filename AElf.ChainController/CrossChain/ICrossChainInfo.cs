using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.ChainController.CrossChain
{
    public interface ICrossChainInfo
    {
        MerklePath GetTxRootMerklePathInParentChain(ulong blockHeight);
        ParentChainBlockInfo GetBoundParentChainBlockInfo(ulong height);
        ulong GetBoundParentChainHeight(ulong localChainHeight);
        ulong GetParentChainCurrentHeight();
    }
}