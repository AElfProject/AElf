using AElf.Kernel;
using AElf.Common;

namespace AElf.Node.CrossChain
{
    public interface ICrossChainInfo
    {
        MerklePath GetTxRootMerklePathInParentChain(Address contractAddress, ulong blockHeight);
        ParentChainBlockInfo GetBoundParentChainBlockInfo(Address contractAddressHash, ulong height);
        ulong GetBoundParentChainHeight(Address contractAddressHash, ulong height);
    }
}