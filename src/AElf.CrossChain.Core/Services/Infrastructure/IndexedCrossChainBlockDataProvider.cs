using System.Collections.Generic;
using System.Linq;
using Acs7;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class IndexedCrossChainBlockDataProvider : IIndexedCrossChainBlockDataProvider, ISingletonDependency
    {
        private readonly Dictionary<Hash, CrossChainBlockData> _indexedCrossChainBlockData =
            new Dictionary<Hash, CrossChainBlockData>();

        public void SetIndexedBlockData(Hash blockHash, CrossChainBlockData crossChainBlockData)
        {
            _indexedCrossChainBlockData[blockHash] = crossChainBlockData;
        }

        public CrossChainBlockData GetIndexedBlockData(Hash blockHash)
        {
            return _indexedCrossChainBlockData.TryGetValue(blockHash, out var crossChainBlockData)
                ? crossChainBlockData
                : null;
        }

        public void ClearExpiredCrossChainBlockData(long blockHeight)
        {
            var toRemoveList = _indexedCrossChainBlockData.Where(kv => kv.Value.PreviousBlockHeight < blockHeight)
                .Select(kv => kv.Key).ToList();
            foreach (var hash in toRemoveList)
            {
                _indexedCrossChainBlockData.Remove(hash);
            }
        }

        public int GetCachedCrossChainBlockDataCount()
        {
            return _indexedCrossChainBlockData.Count;
        }
    }
}