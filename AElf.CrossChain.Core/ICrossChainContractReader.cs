using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.CrossChain
{
    public interface ICrossChainContractReader
    {
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight);
        Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(ulong height);
        Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight);
        Task<ulong> GetParentChainCurrentHeightAsync(int chainId, Hash previousBlockHash,
            ulong preBlockHeight);
        Task<ulong> GetSideChainCurrentHeightAsync(int chainId, int sideChainId, Hash previousBlockHash,
            ulong preBlockHeight);

        Task<int> GetParentChainIdAsync(int chainId, Hash previousBlockHash, ulong preBlockHeight);
        Task<Dictionary<int, ulong>> GetSideChainIdAndHeightAsync(int chainId, Hash previousBlockHash,
            ulong preBlockHeight);
        Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height);
    }
}