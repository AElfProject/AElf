using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.ChainController.CrossChain
{
    public interface ICrossChainInfoReader
    {
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight);
        Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(ulong height);
        Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight);
        Task<ulong> GetParentChainCurrentHeightAsync();
        Task<ulong> GetSideChainCurrentHeightAsync(int chainId);
        Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResult(ulong height);
    }
}