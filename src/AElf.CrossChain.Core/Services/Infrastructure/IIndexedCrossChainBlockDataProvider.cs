using Acs7;
using AElf.Types;

namespace AElf.CrossChain
{
    public interface IIndexedCrossChainBlockDataProvider
    {
        void SetIndexedBlockData(Hash blockHash, CrossChainBlockData crossChainBlockData);
        
        CrossChainBlockData GetIndexedBlockData(Hash blockHash);

        void ClearExpiredCrossChainBlockData(long blockHeight);
    }
}