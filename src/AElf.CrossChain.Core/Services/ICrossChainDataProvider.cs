using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.CrossChain
{
    internal interface ICrossChainDataProvider
    {
        Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight);

        Task<ChainInitializationInformation> GetChainInitializationContextAsync(int chainId, Hash blockHash, long blockHeight);

        Task<List<SideChainBlockData>> GetSideChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight);

        Task<bool> ValidateSideChainBlockDataAsync(List<SideChainBlockData> sideChainBlockDataList,
            Hash currentBlockHash, long currentBlockHeight);

        Task<List<ParentChainBlockData>> GetParentChainBlockDataAsync(Hash currentBlockHash, long currentBlockHeight);

        Task<bool> ValidateParentChainBlockDataAsync(List<ParentChainBlockData> parentChainBlockDataList,
            Hash currentBlockHash, long currentBlockHeight);

        Task<CrossChainBlockData> GetCrossChainBlockDataForNextMiningAsync(Hash currentBlockHash,
            long currentBlockHeight);

        CrossChainBlockData GetUsedCrossChainBlockDataForLastMiningAsync(Hash blockHash, long previousBlockHeight);

        void UpdateWithLibIndex(BlockIndex blockIndex);
    }
}