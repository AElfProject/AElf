using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.ChainController.CrossChain
{
    public interface ICrossChainInfoHelper
    {
        MerklePath GetTxRootMerklePathInParentChain(ulong blockHeight);
        ParentChainBlockInfo GetBoundParentChainBlockInfo(ulong height);
        ulong GetBoundParentChainHeight(ulong localChainHeight);
        ulong GetParentChainCurrentHeight();
        ulong GetSideChainCurrentHeight(Hash chainId);
    }
}