using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Indexing.Infrastructure
{
    internal class TransactionInputForBlockMiningDataProvider : ITransactionInputForBlockMiningDataProvider, ISingletonDependency
    {
        private readonly Dictionary<Hash, CrossChainTransactionInput> _indexedCrossChainBlockData =
            new Dictionary<Hash, CrossChainTransactionInput>();

        public void AddTransactionInputForBlockMining(Hash blockHash, CrossChainTransactionInput crossChainTransactionInput)
        {
            _indexedCrossChainBlockData[blockHash] = crossChainTransactionInput;
        }

        public CrossChainTransactionInput GetTransactionInputForBlockMining(Hash blockHash)
        {
            return _indexedCrossChainBlockData.TryGetValue(blockHash, out var crossChainBlockData)
                ? crossChainBlockData
                : null;
        }

        public void ClearExpiredTransactionInput(long blockHeight)
        {
            var toRemoveList = _indexedCrossChainBlockData.Where(kv => kv.Value.PreviousBlockHeight < blockHeight)
                .Select(kv => kv.Key);
            foreach (var hash in toRemoveList)
            {
                _indexedCrossChainBlockData.Remove(hash);
            }
        }
    }
}