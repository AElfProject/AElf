using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.ChainController.CrossChain
{
    public interface ICrossChainInfoReader
    {
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(int chainId, ulong blockHeight);
        Task<ParentChainBlockInfo> GetBoundParentChainBlockInfoAsync(int chainId, ulong height);
        Task<ulong> GetBoundParentChainHeightAsync(int chainId, ulong localChainHeight);
        Task<ulong> GetParentChainCurrentHeightAsync(int chainId);
        Task<ulong> GetSideChainCurrentHeightAsync(int chainId, int sideChainId);
        Task<IndexedSideChainBlockInfoResult> GetIndexedSideChainBlockInfoResult(int chainId, ulong height);
    }
}