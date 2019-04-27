using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.CrossChain
{
    public interface ICrossChainDataProvider
    {
        Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight);

        Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockData,
            Hash currentBlockHash, long currentBlockHeight);

        Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight);

        Task<bool> ValidateParentChainBlockDataAsync(List<ParentChainBlockData> parentChainBlockData,
            Hash currentBlockHash, long currentBlockHeight);
        
        Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight);

        Task<CrossChainBlockData> GetCrossChainBlockDataForNextMiningAsync(Hash currentBlockHash, long currentBlockHeight);

        CrossChainBlockData GetUsedCrossChainBlockDataForLastMiningAsync(Hash blockHash, long previousBlockHeight);

        Task<ChainInitializationContext> GetChainInitializationContextAsync(int chainId, Hash blockHash, long blockHeight);
        Task HandleNewLibAsync(LastIrreversibleBlockDto lastIrreversibleBlockDto);
    }
}