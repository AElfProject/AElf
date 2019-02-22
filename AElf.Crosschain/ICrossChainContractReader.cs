using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Crosschain
{
    public interface ICrossChainContractReader
    {
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight);
        Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(ulong height);
        Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight);
        Task<ulong> GetParentChainCurrentHeightAsync(int chainId, int parentChainId);
        Task<ulong> GetSideChainCurrentHeightAsync(int chainId, int sideChainId);
        Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height);
    }
}