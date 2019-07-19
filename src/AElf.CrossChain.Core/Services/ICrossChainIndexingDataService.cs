using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.Types;
using AElf.Kernel;

namespace AElf.CrossChain
{
    public interface ICrossChainIndexingDataService
    {
        Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash blockHash, long blockHeight);
        
        Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId, Hash blockHash, long blockHeight);

        Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockDataList,
            Hash blockHash, long blockHeight);

        Task<bool> ValidateParentChainBlockDataAsync(List<ParentChainBlockData> parentChainBlockDataList,
            Hash blockHash, long blockHeight);

        Task<CrossChainBlockData> GetCrossChainBlockDataForNextMiningAsync(Hash blockHash,
            long blockHeight);

        CrossChainBlockData GetUsedCrossChainBlockDataForLastMiningAsync(Hash blockHash, long blockHeight);

        void UpdateCrossChainDataWithLibIndex(BlockIndex blockIndex);
    }
}