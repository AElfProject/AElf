using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.ChainController.CrossChain
{
    public interface ICrossChainInfoReader
    {
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight);
        Task<ParentChainBlockInfo> GetBoundParentChainBlockInfoAsync(ulong height);
        Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight);
        Task<ulong> GetParentChainCurrentHeightAsync();
        Task<ulong> GetSideChainCurrentHeightAsync(Hash chainId);
        Task<BinaryMerkleTree> GetMerkleTreeForSideChainTransactionRootAsync(ulong height);
    }
}