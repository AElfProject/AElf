using AElf.Kernel;

namespace AElf.Node.CrossChain
{
    public interface ICrossChainInfo
    {
        MerklePath GetTxRootMerklePathInParentChain(Hash contractAddress, ulong blockHeight);
    }
}